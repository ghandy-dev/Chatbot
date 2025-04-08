module Bot

open Authorization
open Commands
open Configuration
open Clients
open Database
open IRC
open IRC.Messages.Types
open MessageHandlers
open Shared
open Types
open Twitch

open System
open System.Collections.Generic
open System.Collections.Concurrent

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

let reminderAgent (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<ReminderMessage>(
        (fun mb ->
            let checkReminders () =
                async {
                    let! reminders = ReminderRepository.getTimedReminders ()

                    for reminder in reminders do
                        let ts = DateTime.UtcNow - reminder.Timestamp
                        let sender = if reminder.FromUsername = reminder.TargetUsername then "yourself" else $"@%s{reminder.FromUsername}"
                        let message = $"@%s{reminder.TargetUsername}, reminder from %s{sender} (%s{formatTimeSpan ts} ago): %s{reminder.Message}" |> formatChatMessage
                        do! twitchChatClient.SendAsync(IRC.Command.PrivMsg(reminder.Channel, message))

                    do! Async.Sleep(250)
                    mb.Post CheckReminders
                }

            let userMessaged channel userId username =
                async {
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
                }

            let rec loop () =
                async {
                    match! mb.Receive() with
                    | ReminderMessage.CheckReminders -> do! checkReminders ()
                    | ReminderMessage.UserMessaged(channel, userId, username) -> do! userMessaged channel userId username

                    return! loop ()
                }

            Logging.trace "Reminder agent started."
            mb.Post CheckReminders
            loop ()
        ), cancellationToken
    )

let triviaAgent (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<TriviaRequest>(
        (fun mb ->
            let trivias = new ConcurrentDictionary<string, TriviaConfig> ()
            let active = new ConcurrentDictionary<string, bool> ()
            let timestamps = new ConcurrentDictionary<string, DateTime> ()
            let sentHints = new ConcurrentDictionary<string, Set<int>>()
            let answerSent = new ConcurrentDictionary<string, bool> ()

            let sendMessage channel message = twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, message))

            let startTrivia (trivia: TriviaConfig) =
                async {
                    match active |> Dictionary.tryGetValue trivia.Channel with
                    | Some true -> do! sendMessage trivia.Channel "Trivia already started"
                    | _ ->
                        active[trivia.Channel] <- true
                        trivias[trivia.Channel] <- trivia
                        mb.Post (SendQuestion trivia)
                }

            let stopTrivia channel =
                async {
                    match active |> Dictionary.tryGetValue channel with
                    | Some true ->
                        active[channel] <-  false
                        do! sendMessage channel "Trivia stopped"
                    | _ -> ()
                }

            let update () =
                async {
                    Seq.zip3 active timestamps trivias
                    |> Seq.choose (fun (KeyValue(channel, active), (KeyValue(channel, timestamp)), (KeyValue(channel, trivia))) ->
                        if active then Some (channel, utcNow() - timestamp, trivia) else None
                    )
                    |> Seq.iter (fun (channel, timespan, trivia) ->
                        let elapsedSeconds = int timespan.TotalSeconds
                        let hintTimes = [ 15 ; 30 ]
                        let answerTime = 55
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
                }

            let sendQuestion trivia =
                async {
                    match trivia.Questions with
                    | q :: _ ->
                        do! sendMessage trivia.Channel $"[Trivia - %s{q.Category}] Question: %s{q.Question}"
                        timestamps[trivia.Channel] <- utcNow()
                        sentHints[trivia.Channel] <- Set.empty
                        answerSent[trivia.Channel] <- false
                    | _ -> ()
                }

            let sendHint trivia =
                async {
                    match trivia.Questions with
                    | q :: qs ->
                        match q.Hints with
                        | h :: hs ->
                            do! sendMessage trivia.Channel $"[Trivia] Hint: %s{h}"
                            trivias[trivia.Channel] <- { trivia with Questions = { q with Hints = hs } :: qs }
                        | _ -> ()
                    | _ -> ()
                }

            let sendAnswer trivia =
                async {
                    match trivia.Questions with
                    | [ q ] ->
                        do! sendMessage trivia.Channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                        active[trivia.Channel] <- false
                    | q :: qs ->
                        do! sendMessage trivia.Channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                        let trivia = { trivia with Questions = qs }
                        trivias[trivia.Channel] <- trivia
                        mb.Post (SendQuestion trivia)
                    | _ -> ()
                }

            let userMessaged channel userId username message =
                async {
                    match active |> Dictionary.tryGetValue channel with
                    | Some true ->
                        let trivia = trivias[channel]
                        match trivia.Questions with
                        | q :: qs when String.Compare(q.Answer, message, ignoreCase = true) = 0  ->
                            do! sendMessage trivia.Channel $"""[Trivia] @%s{username}, got it! The answer was %s{q.Answer}"""

                            if qs.IsEmpty then
                                active[trivia.Channel] <- false
                            else
                                let trivia = { trivia with Questions = qs }
                                trivias[trivia.Channel] <- trivia
                                mb.Post (SendQuestion trivia)
                        | _ -> ()
                    | _ -> ()
                }

            let rec loop () =
                async {
                    match! mb.Receive() with
                    | TriviaRequest.StartTrivia trivia -> do! startTrivia trivia
                    | TriviaRequest.StopTrivia channel -> do! stopTrivia channel
                    | TriviaRequest.Update -> do! update ()
                    | TriviaRequest.SendQuestion trivia -> do! sendQuestion trivia
                    | TriviaRequest.SendHint trivia -> do! sendHint trivia
                    | TriviaRequest.SendAnswer trivia -> do! sendAnswer trivia
                    | TriviaRequest.UserMessaged (channel, userId, username, message) -> do! userMessaged channel userId username message

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
                        Logging.info "Reconnecting..."
                        do! twitchChatClient.ReconnectAsync(cancellationToken)
                        let! channels = getChannelJoinList ()
                        do! twitchChatClient.SendAsync(IRC.JoinM (channels |> Seq.map snd))
                    }

                let rec reconnectHelper () =
                    async {
                        if (DateTime.UtcNow - lastPingTime).Seconds > 360 then
                            lastPingTime <- DateTime.UtcNow
                            mb.Post Reconnect

                        do! Async.Sleep(1000)
                        do! reconnectHelper ()
                    }

                let sendPong pong =
                    async {
                        Logging.info $"PONG :{pong}"
                        lastPingTime <- DateTime.UtcNow
                        do! twitchChatClient.SendAsync(IRC.Command.Pong pong)
                    }

                let sendPrivateMessage channel message = twitchChatClient.SendAsync(IRC.Command.PrivMsg(channel, message))

                let sendReplyMessage messageId channel message = twitchChatClient.SendAsync(IRC.Command.ReplyMsg(messageId, channel, message))

                let sendRawIrcMessage message = twitchChatClient.SendAsync(IRC.Command.Raw message)

                let sendWhisper userId message =
                    async {
                        match! tokenStore.GetToken TokenType.Twitch with
                        | None -> Logging.info "Unable to send whisper. Couldn't retrieve access token for Twitch API."
                        | Some token -> do! twitchChatClient.WhisperAsync(userId, message, token)
                    }

                let handleBotCommand command =
                    async {
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
                    }

                let rec loop () =
                    async {
                        match! mb.Receive() with
                        | ClientRequest.SendPongMessage pong -> do! sendPong pong
                        | ClientRequest.SendPrivateMessage(channel, message) -> do! sendPrivateMessage channel message
                        | ClientRequest.SendWhisperMessage(userId, _, message) -> do! sendWhisper userId message
                        | ClientRequest.SendReplyMessage(messageId, channel, message) -> do! sendReplyMessage messageId channel message
                        | ClientRequest.SendRawIrcMessage message -> do! sendRawIrcMessage message
                        | ClientRequest.BotCommand command -> do! handleBotCommand command
                        | ClientRequest.Reconnect -> do! reconnect()

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
