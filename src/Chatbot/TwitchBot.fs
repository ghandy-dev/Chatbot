module Bot

open Authorization
open Commands
open Configuration
open Clients
open Database
open IRC
open IRC.Messages.Types
open MessageHandlers
open State
open Types
open Twitch

open System

let getAccessToken () =
    async {
        match! tokenStore.GetToken(TokenType.Twitch) with
        | None -> return failwith "Failed to get access token"
        | Some token -> return token
    }

let getAccessTokenUser token =
    async {
        match! Helix.Users.getAccessTokenUser token with
        | Some user -> return user
        | None -> return failwith "Failed to look up user associated with access token"
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
                    match! ReminderRepository.getPendingReminderCount userId with
                    | DatabaseResult.Failure -> ()
                    | DatabaseResult.Success c when c = 0 -> ()
                    | DatabaseResult.Success _ ->
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
                            | Ok url -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, $"@{username}, reminders were too long to send, check {url} for your reminders"))
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

let chatAgent (twitchChatClient: TwitchChatClient) (user: TTVSharp.Helix.User) cancellationToken =
    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {
                let mutable lastPingTime = DateTime.UtcNow

                let reconnect () =
                    async {
                        do! twitchChatClient.ReconnectAsync(cancellationToken)
                        let! channels = getChannelJoinList ()
                        do! joinChannels twitchChatClient (channels |> Seq.map snd)
                    }

                let rec reconnectHelper () =
                    async {
                        if (DateTime.UtcNow - lastPingTime).Seconds > 360 then
                            mb.Post Reconnect
                        else
                            do! Async.Sleep(1000)
                            do! reconnectHelper ()
                    }

                let rec loop () =
                    async {
                        match! mb.Receive() with
                        | SendPongMessage pong ->
                            Logging.info $"PONG :{pong}"
                            lastPingTime <- DateTime.UtcNow
                            do! twitchChatClient.SendAsync(IRC.Command.Pong pong)
                        | SendPrivateMessage(channel, message) -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, message))
                        | SendWhisperMessage(userId, username, message) ->
                            match! tokenStore.GetToken TokenType.Twitch with
                            | None -> Logging.info "Unable to send whisper. Couldn't retrieve access token for Twitch API."
                            | Some token -> do! twitchChatClient.WhisperAsync(userId, message, token)
                        | SendReplyMessage(messageId, channel, message) -> do! twitchChatClient.SendAsync(IRC.Command.ReplyMsg(messageId, channel, message))
                        | SendRawIrcMessage msg -> do! twitchChatClient.SendAsync(IRC.Command.Raw msg)
                        | BotCommand command ->
                            match command with
                            | JoinChannel (channel, channelId) ->
                                do! emoteService.RefreshChannelEmotes channelId
                                do! twitchChatClient.SendAsync(IRC.Command.Join channel)
                            | LeaveChannel channel -> do! twitchChatClient.SendAsync(IRC.Command.Part channel)
                            | RefreshGlobalEmotes provider ->
                                let! accessToken = getAccessToken ()
                                do! emoteService.RefreshGlobalEmotes(user.Id, accessToken)
                            | RefreshChannelEmotes channelId -> do! emoteService.RefreshChannelEmotes(channelId)
                        | Reconnect ->
                            Logging.info "Reconnecting..."
                            do! reconnect()
                            do! loop ()

                        do! loop ()
                    }

                Async.Start (reconnectHelper (), cancellationToken)
                do! loop ()
            }
        ),
        cancellationToken
    )

let run (cancellationToken: Threading.CancellationToken) =
    async {
        let! accessToken = getAccessToken ()
        let! user = getAccessTokenUser accessToken

        let connectionConfig =
            match appConfig.Bot.ConnectionProtocol with
            | "irc" ->
                let uri = new Uri(appConfig.ConnectionStrings.TwitchIrc)
                ConnectionType.IRC(uri.Host, uri.Port)
            | "wss" ->
                let uri = new Uri(appConfig.ConnectionStrings.TwitchWss)
                ConnectionType.Websocket(uri.Host, uri.Port)
            | _ -> failwith "Unsupported connection protocol set"

        let twitchChatConfig: TwitchChatClientConfig = {
            UserId = user.Id
            Username = user.DisplayName
            Capabilities = appConfig.Bot.Capabilities
        }

        let twitchChatClient = new TwitchChatClient(connectionConfig, twitchChatConfig)

        let chatAgent = chatAgent twitchChatClient user cancellationToken
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
        do! emoteService.RefreshGlobalEmotes(user.Id, accessToken)
        let refreshChannelEmotes = channels |> Seq.map fst |> Seq.map (fun c -> emoteService.RefreshChannelEmotes c)
        do! refreshChannelEmotes |> Async.Parallel |> Async.Ignore

        chatAgent.Start()
        reminderAgent.Start()
    }
