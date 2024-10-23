module Bot

open Authorization
open Commands
open Clients
open Database
open IRC
open IRC.Messages.Types
open MessageHandlers
open State
open Types
open Twitch

open System

let botConfig = Configuration.Bot.config

let getAccessTokenUser () =
    async {
        match! tokenStore.GetToken(TokenType.Twitch) |> Option.bindAsync Helix.Users.getAccessTokenUser with
        | Some user -> return user
        | None -> return failwith "Expect access token (or user?)"
    }

let getChannelJoinList () =
    async {
        let! channels = ChannelRepository.getAll ()

        match!
            channels
            |> Seq.map (fun c -> c.ChannelId)
            |> Async.create
            |> Async.bind Twitch.Helix.Users.getUsersById
        with
        | None ->
            Logging.warning "Twitch API error, falling back on database channel names"
            return channels |> Seq.map (fun c -> c.ChannelId, c.ChannelName)
        | Some channels ->
            return channels |> Seq.map (fun u -> u.Id, u.Login)
    }

let connectionConfig =
    let uri = new Uri(Configuration.ConnectionStrings.config.Twitch)

    match uri.Scheme with
    | "irc" -> ConnectionType.IRC(uri.Host, uri.Port)
    | "wss" -> ConnectionType.Websocket(uri.Host, uri.Port)
    | _ -> failwith "Unknown protocol used for twitch connection string"

type ReminderMessage =
    | CheckReminders
    | UserTyped of channel: string * userId: int * username: string

let reminderAgent (twitchChatClient: TwitchChatClient) =
    new MailboxProcessor<ReminderMessage>(fun mb ->
        let rec loop () =
            async {
                match! mb.Receive() with
                | CheckReminders ->
                    let! reminders = ReminderRepository.getTimedReminders ()

                    for reminder in reminders do
                        let ts = DateTime.UtcNow - reminder.Timestamp
                        let sender = if reminder.FromUsername = reminder.TargetUsername then "yourself" else $"@{reminder.FromUsername}"
                        let reminderMessage = $"@{reminder.TargetUsername}, reminder from {sender} ({formatTimeSpan ts} ago): {reminder.Message}" |> formatChatMessage
                        do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(reminder.Channel, reminderMessage))

                    do! Async.Sleep(250)
                    mb.Post CheckReminders
                | UserTyped(channel, userId, username) ->
                    let! reminderCount = ReminderRepository.getPendingReminderCount userId

                    if reminderCount = 0 then
                        ()
                    else
                        let! reminders = ReminderRepository.getReminders userId

                        let reminderMessages =
                            reminders
                            |> Seq.map (fun r ->
                                let ts = DateTime.UtcNow - r.Timestamp
                                let sender = if r.FromUsername = r.TargetUsername then "yourself" else $"@{r.FromUsername}"
                                $" reminder from {sender} ({formatTimeSpan ts} ago): {r.Message}"
                            )
                            |> String.concat ", "

                        if reminderMessages.Length > 500 then
                            match! Pastebin.createPaste "" reminderMessages with
                            | Error err -> Logging.error err (new Exception())
                            | Ok url -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, $"@{username}, reminders were too long to send in chat, check {url} for your reminders"))
                        else
                            do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, $"@{username}, {reminderMessages}"))

                return! loop ()
            }

        Logging.trace "Reminder agent started."
        mb.Post CheckReminders
        loop ()
    )

let joinChannels (twitchChatClient: TwitchChatClient) channels =
    async {
        do! twitchChatClient.SendAsync(IRC.JoinM channels)
    }

let getChannelEmotes channels =
    async {
        channels |> Seq.map (fun c -> emoteService.RefreshChannelEmotes c) |> Async.Parallel |> ignore
    }

let chatAgent (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {
                let reconnect () =
                    async {
                            do! twitchChatClient.ReconnectAsync(cancellationToken)
                            let! channels = getChannelJoinList ()
                            do! joinChannels twitchChatClient (channels |> Seq.map snd)
                    }

                let rec loop () =
                    async {
                        match! mb.Receive() with
                        | SendPongMessage pong ->
                            Logging.info $"PONG :{pong}"
                            do! twitchChatClient.SendAsync(IRC.Command.Pong pong)
                        | SendPrivateMessage(channel, message) -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, message))
                        | SendWhisperMessage(userId, username, message) ->
                            match! tokenStore.GetToken TokenType.Twitch with
                            | None -> Logging.info "Unable to send whisper. Couldn't retrieve access token for Twitch API."
                            | Some token -> do! twitchChatClient.WhisperAsync(userId, message, token)
                        | SendRawIrcMessage msg -> do! twitchChatClient.SendAsync(IRC.Command.Raw msg)
                        | BotCommand command ->
                            match command with
                            | JoinChannel (channel, channelId) ->
                                do! emoteService.RefreshChannelEmotes channelId
                                do! twitchChatClient.SendAsync(IRC.Command.Join channel)
                            | LeaveChannel channel -> do! twitchChatClient.SendAsync(IRC.Command.Part channel)
                            | RefreshGlobalEmotes provider -> do! emoteService.RefreshGlobalEmotes provider
                            | RefreshChannelEmotes(channelId, provider) -> do! emoteService.RefreshChannelEmotes(channelId, provider)
                        | Reconnect ->
                            Logging.info "Twitch servers requested we reconnect..."
                            do! reconnect()
                            do! loop ()

                        do! loop ()
                    }

                do! loop ()
            }
        ),
        cancellationToken
    )

let run (cancellationToken: Threading.CancellationToken) =
    async {
        let! user = getAccessTokenUser()

        let twitchChatConfig: TwitchChatClientConfig = {
            UserId = user.Id
            Username = user.DisplayName
            Capabilities = botConfig.Capabilities
        }

        let twitchChatClient = new TwitchChatClient(connectionConfig, twitchChatConfig)

        let chatAgent = chatAgent twitchChatClient cancellationToken
        let reminderAgent = reminderAgent twitchChatClient

        twitchChatClient.MessageReceived.Subscribe(fun messages -> handleMessages messages chatAgent |> Async.Start) |> ignore

        twitchChatClient.MessageReceived
        |> Event.filter (fun messages ->
            messages
            |> Array.exists (
                function
                | PrivateMessage _ -> true
                | _ -> false
            )
        )
        |> Event.add (fun messages ->
            messages
            |> Array.iter (
                function
                | PrivateMessage msg -> reminderAgent.Post(UserTyped(msg.Channel, msg.UserId |> int, msg.Username))
                | _ -> ()
            )
        )
        |> ignore

        do! twitchChatClient.StartAsync(cancellationToken)

        let! channels = getChannelJoinList ()
        do! joinChannels twitchChatClient (channels |> Seq.map snd)
        do! emoteService.RefreshGlobalEmotes ()
        do! getChannelEmotes (channels |> Seq.map fst)

        chatAgent.Start()
        reminderAgent.Start()
    }
