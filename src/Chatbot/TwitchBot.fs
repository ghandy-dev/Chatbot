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
open System.Collections.Generic

let services = Services.services

let getAccessToken () =
    async {
        match! tokenStore.GetToken(TokenType.Twitch) with
        | None -> return failwith "Failed to get access token"
        | Some token -> return token
    }

let getAccessTokenUser token =
    async {
        match! Helix.Users.getAccessTokenUser token with
        | None -> return failwith "Failed to look up user associated with access token"
        | Some user -> return user
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
    | UserMessaged of channel: string * userId: int * username: string

let reminderAgent (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<ReminderMessage>(
        (fun mb ->
            let rec loop () =
                async {
                    match! mb.Receive() with
                    | CheckReminders ->
                        let! reminders = ReminderRepository.getTimedReminders ()

                        for reminder in reminders do
                            let ts = DateTime.UtcNow - reminder.Timestamp
                            let sender = if reminder.FromUsername = reminder.TargetUsername then "yourself" else $"@%s{reminder.FromUsername}"
                            let message = $"@%s{reminder.TargetUsername}, reminder from %s{sender} (%s{formatTimeSpan ts} ago): %s{reminder.Message}" |> formatChatMessage
                            do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(reminder.Channel, message))

                        do! Async.Sleep(250)
                        mb.Post CheckReminders
                    | UserMessaged(channel, userId, username) ->
                        match! ReminderRepository.getPendingReminderCount userId with
                        | DatabaseResult.Failure -> ()
                        | DatabaseResult.Success c when c = 0 -> ()
                        | DatabaseResult.Success _ ->
                            let! reminders = ReminderRepository.getReminders userId

                            let message =
                                reminders
                                |> Seq.groupBy (fun r -> r.FromUsername)
                                |> Seq.map (fun (_, rs) ->
                                    let sender = rs |> Seq.head |> fun r -> if r.FromUsername = r.TargetUsername then "yourself" else $"@%s{r.FromUsername}"

                                    let message =
                                        rs
                                        |> Seq.map (fun r ->
                                            let ts = DateTime.UtcNow - r.Timestamp
                                            $"(%s{formatTimeSpan ts} ago): %s{r.Message}"
                                        )
                                        |> String.concat ", "

                                    if rs |> Seq.length = 1 then
                                        $"reminder from %s{sender} %s{message}"
                                    else
                                        $"reminders from %s{sender} %s{message}"
                                )
                                |> String.concat ", "

                            if message.Length > 500 then
                                match! Pastebin.createPaste "" message with
                                | Error (err, statusCode) -> Logging.error err (new Exception())
                                | Ok url -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, $"@%s{username}, reminders were too long to send, check %s{url} for your reminders"))
                            else
                                do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, $"@%s{username}, %s{message}"))

                    return! loop ()
                }

            Logging.trace "Reminder agent started."
            mb.Post CheckReminders
            loop ()
        ), cancellationToken
    )

type TriviaRequest =
    | StartTrivia of Commands.TriviaConfig
    | StopTrivia of channel: string
    | SendQuestion of trivia: Commands.TriviaConfig
    | SendHint of trivia: Commands.TriviaConfig
    | SendAnswer of trivia: Commands.TriviaConfig
    | Update
    | UserMessaged of channel: string * userId: int * username: string * message: string

let triviaAgent (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<_>(
        (fun mb ->
            let channels = new Dictionary<string, TriviaConfig> ()
            let active = new Dictionary<string, bool> ()
            let timestamps = new Dictionary<string, DateTime> ()
            let sentHints = new Dictionary<string, Set<int>>()
            let answerSent = new Dictionary<string, bool> ()

            let rec loop () =
                async {
                    match! mb.Receive() with
                    | StartTrivia trivia ->
                        match active |> Dictionary.tryGetValue trivia.Channel with
                        | None
                        | Some false ->
                            active[trivia.Channel] <- true
                            channels[trivia.Channel] <- trivia
                            mb.Post (SendQuestion trivia)
                        | Some true -> do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, "Trivia already started"))
                    | StopTrivia channel ->
                        match active |> Dictionary.tryGetValue channel with
                        | None
                        | Some false -> ()
                        | Some true ->
                            active[channel] <-  false
                            do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, "Trivia stopped"))
                    | Update ->
                        Seq.zip3 active timestamps channels
                        |> Seq.choose (fun (KeyValue(channel, active), (KeyValue(channel, timestamp)), (KeyValue(channel, trivia))) ->
                            if active then Some (channel, utcNow() - timestamp, trivia) else None
                        )
                        |> Seq.iter (fun (channel, timespan, trivia) ->
                            let elapsedSeconds = int timespan.TotalSeconds
                            let hintTimes = [ 15 ; 30 ]
                            let answerTime = 60
                            let hints = sentHints[trivia.Channel]

                            if elapsedSeconds = answerTime && not <| answerSent[trivia.Channel] then
                                answerSent[trivia.Channel] <- true
                                mb.Post (SendAnswer trivia)
                            else if hintTimes |> List.contains elapsedSeconds && not <| hints.Contains elapsedSeconds then
                                sentHints[trivia.Channel] <- hints |> Set.add elapsedSeconds
                                mb.Post(SendHint trivia)
                        )

                        do! Async.Sleep(250)
                        mb.Post Update
                    | SendQuestion trivia ->
                        match trivia.Questions with
                        | q :: _ ->
                            do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, $"[Trivia - %s{q.Category}] Question: %s{q.Question}"))
                            timestamps[trivia.Channel] <- utcNow()
                            sentHints[trivia.Channel] <- Set.empty
                            answerSent[trivia.Channel] <- false
                        | _ -> ()
                    | SendHint trivia ->
                        match trivia.Questions with
                        | q :: qs ->
                            match q.Hints with
                            | h :: hs ->
                                do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, $"[Trivia] Hint: %s{h}"))
                                channels[trivia.Channel] <- { trivia with Questions = { q with Hints = hs } :: qs }
                            | _ -> ()
                        | _ -> ()
                    | SendAnswer trivia ->
                        match trivia.Questions with
                        | [ q ] ->
                            do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, $"[Trivia] No one got it. The answer was: %s{q.Answer}"))
                            active[trivia.Channel] <- false
                        | q :: qs ->
                            do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, $"[Trivia] No one got it. The answer was: %s{q.Answer}"))
                            let trivia = { trivia with Questions = qs }
                            channels[trivia.Channel] <- trivia
                            mb.Post (SendQuestion trivia)
                        | _ -> ()
                    | UserMessaged (channel, userId, username, message) ->
                        match active |> Dictionary.tryGetValue channel with
                        | Some true ->
                            let trivia = channels[channel]
                            match trivia.Questions with
                            | [ q ] ->
                                if String.Compare(q.Answer, message, ignoreCase = true) = 0 then
                                    do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, $"""[Trivia] @%s{username}, got it! The answer was %s{q.Answer}"""))
                                    active[trivia.Channel] <- false
                            | q :: qs ->
                                if String.Compare(q.Answer, message, ignoreCase = true) = 0 then
                                    do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(trivia.Channel, $"""[Trivia] @%s{username}, got it! The answer was %s{q.Answer}"""))
                                    let trivia = { trivia with Questions = qs }
                                    channels[trivia.Channel] <- trivia
                                    mb.Post (SendQuestion trivia)
                            | _ -> ()
                        | _ -> ()

                    return! loop ()
                }

            Logging.trace "Trivia agent started."
            mb.Post Update
            loop ()
        ),
        cancellationToken
    )

let chatAgent (twitchChatClient: TwitchChatClient) (user: TTVSharp.Helix.User) (triviaAgent: MailboxProcessor<TriviaRequest>) cancellationToken =
    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {
                let mutable lastPingTime = DateTime.UtcNow

                let reconnect () =
                    async {
                        do! twitchChatClient.ReconnectAsync(cancellationToken)
                        let! channels = getChannelJoinList ()
                        do! twitchChatClient.SendAsync(IRC.JoinM (channels |> Seq.map snd))
                    }

                let rec reconnectHelper () =
                    async {
                        if (DateTime.UtcNow - lastPingTime).Seconds > 360 then
                            lastPingTime <- DateTime.UtcNow
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
                                do! services.EmoteService.RefreshChannelEmotes channelId
                                do! twitchChatClient.SendAsync(IRC.Command.Join channel)
                            | LeaveChannel channel -> do! twitchChatClient.SendAsync(IRC.Command.Part channel)
                            | RefreshGlobalEmotes provider ->
                                let! accessToken = getAccessToken ()
                                do! services.EmoteService.RefreshGlobalEmotes(user.Id, accessToken)
                            | RefreshChannelEmotes channelId -> do! services.EmoteService.RefreshChannelEmotes(channelId)
                            | BotCommand.StartTrivia trivia -> triviaAgent.Post (TriviaRequest.StartTrivia trivia)
                            | BotCommand.StopTrivia channel -> triviaAgent.Post (TriviaRequest.StopTrivia channel)
                        | Reconnect ->
                            Logging.info "Reconnecting..."
                            do! reconnect()

                        return! loop ()
                    }

                Async.Start (reconnectHelper (), cancellationToken)
                return! loop ()
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

        let triviaAgent = triviaAgent twitchChatClient cancellationToken
        let chatAgent = chatAgent twitchChatClient user triviaAgent cancellationToken
        let reminderAgent = reminderAgent twitchChatClient cancellationToken

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
                | PrivateMessage msg ->
                    reminderAgent.Post(ReminderMessage.UserMessaged(msg.Channel, msg.UserId |> int, msg.Username))
                    triviaAgent.Post(TriviaRequest.UserMessaged(msg.Channel, msg.UserId |> int, msg.Username, msg.Message))
                | _ -> ()
            )
        )
        |> ignore

        do! twitchChatClient.StartAsync(cancellationToken)

        let! channels = getChannelJoinList ()
        do! twitchChatClient.SendAsync(IRC.JoinM (channels |> Seq.map snd))
        do! services.EmoteService.RefreshGlobalEmotes(user.Id, accessToken)
        let refreshChannelEmotes = channels |> Seq.map fst |> Seq.map (fun c -> services.EmoteService.RefreshChannelEmotes c)
        do! refreshChannelEmotes |> Async.Parallel |> Async.Ignore

        chatAgent.Start()
        reminderAgent.Start()
        triviaAgent.Start()
    }
