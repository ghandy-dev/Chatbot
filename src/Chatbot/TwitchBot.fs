module Chatbot.Bot

open Chatbot
open Chatbot.Commands
open Chatbot.Database
open Chatbot.IRC
open Chatbot.IRC.Messages.Types
open Chatbot.MessageHandlers
open Chatbot.Types

open Authorization
open TTVSharp

open System

let botConfig = Configuration.Bot.config

let user =
    async {
        match! tokenStore.GetToken(TokenType.Twitch) |> Option.bindAsync Helix.Users.getAccessTokenUser with
        | Some user -> return user
        | None -> return failwith "Expect access token (or user?)"
    }
    |> Async.RunSynchronously

let connectionConfig =
    let uri = new Uri(Configuration.ConnectionStrings.config.Twitch)

    match uri.Scheme with
    | "irc" -> ConnectionType.IRC(uri.Host, uri.Port)
    | "wss" -> ConnectionType.Websocket(uri.Host, uri.Port)
    | _ -> failwith "Bad connection string - expected format: scheme://host:port"

let twitchChatConfig: TwitchChatClientConfig = {
    Username = user.DisplayName
    Capabilities = botConfig.Capabilities
}

let getChannels () =
    async { return! ChannelRepository.getAll () |-> List.ofSeq |-> List.map (fun c -> c.ChannelName) }

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
                        let sender = if (reminder.FromUsername = reminder.TargetUsername) then "yourself" else $"@{reminder.FromUsername}"
                        let reminderMessage = $"@{reminder.TargetUsername}, reminder from {sender} ({formatTimeSpan ts} ago): {reminder.Message}" |> formatChatMessage
                        do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(reminder.Channel, reminderMessage))

                    do! Async.Sleep(1000)
                    mb.Post(CheckReminders)
                | UserTyped(channel, userId, username) ->
                    let! reminderCount = ReminderRepository.getPendingReminderCount userId

                    if (reminderCount = 0) then
                        ()
                    else
                        let! reminders = ReminderRepository.getReminders userId

                        let reminderMessages =
                            reminders
                            |> Seq.map (fun r ->
                                let ts = DateTime.UtcNow - r.Timestamp
                                let sender = if (r.FromUsername = r.TargetUsername) then "yourself" else $"@{r.FromUsername}"
                                $" reminder from {sender} ({formatTimeSpan ts} ago): {r.Message}"
                            )
                            |> String.concat ", "
                            |> formatChatMessage

                        do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, $"@{username}, {reminderMessages}"))

                return! loop ()
            }

        Logging.trace "Reminder agent started."
        mb.Post(CheckReminders)
        loop ()
    )

let joinChannels (twitchChatClient: TwitchChatClient) =
    async {
        let! channels = getChannels ()
        do! twitchChatClient.SendAsync(IRC.JoinM channels)
    }

let chatAgent (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {


                let rec loop () =
                    async {
                        match! mb.Receive() with
                        | SendPongMessage pong ->
                            Logging.info $"PONG :{pong}"
                            do! twitchChatClient.SendAsync(IRC.Command.Pong pong)
                        | SendPrivateMessage(channel, message) -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, message))
                        | SendWhisperMessage(userId, username, message) ->
                            match! tokenStore.GetToken TokenType.Twitch with
                            | None -> Logging.info "Failed to send whisper. Couldn't retrieve access token for Twitch API."
                            | Some token ->
                                match! Helix.Whispers.sendWhisper user.Id userId message token with
                                | 204 -> Logging.info $"Whisper sent to {username}: {message}"
                                | statusCode -> Logging.info $"Failed to send whisper, response from Helix Whisper API: {statusCode}"
                        | SendRawIrcMessage msg -> do! twitchChatClient.SendAsync(IRC.Command.Raw msg)
                        | BotCommand(JoinChannel channel) -> do! twitchChatClient.SendAsync(IRC.Command.Join channel)
                        | BotCommand(LeaveChannel channel) -> do! twitchChatClient.SendAsync(IRC.Command.Part channel)
                        | Reconnect ->
                            Logging.info "Twitch servers requested we reconnect..."
                            do! twitchChatClient.ReconnectAsync(cancellationToken)
                            do! joinChannels (twitchChatClient)
                            do! loop ()

                        do! loop ()
                    }

                do! loop ()

            }
        ),
        cancellationToken
    )

let run (cancellationToken: Threading.CancellationToken) =
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

    twitchChatClient.StartAsync(cancellationToken) |> Async.RunSynchronously
    joinChannels(twitchChatClient) |> Async.RunSynchronously

    chatAgent.StartImmediate()
    reminderAgent.StartImmediate()
