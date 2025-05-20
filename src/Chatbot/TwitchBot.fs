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
                        do! twitchChatClient.SendAsync(Commands.PrivMsg(reminder.Channel, message))

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
                            | Ok url -> do! twitchChatClient.SendAsync(Commands.PrivMsg(channel, $"@%s{username}, reminders were too long to send, check %s{url} for your reminders"))
                        else
                            do! twitchChatClient.SendAsync(Commands.PrivMsg(channel, $"@%s{username}, %s{message}"))
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
            let sendMessage channel message = twitchChatClient.SendAsync(Commands.PrivMsg(channel, message))

            let startTrivia (trivia: TriviaConfig) state =
                async {
                    match state |> Map.containsKey trivia.Channel with
                    | true ->
                        do! sendMessage trivia.Channel "Trivia already started"
                        return state
                    | false ->
                        let state = state |> Map.add trivia.Channel { trivia with Timestamp = utcNow() }
                        mb.Post (SendQuestion trivia.Channel)
                        mb.Post Update
                        return state
                }

            let stopTrivia channel state =
                async {
                    match state |> Map.containsKey channel with
                    | true ->
                        do! sendMessage channel "Trivia stopped"
                        return state |> Map.remove channel
                    | false -> return state
                }

            let update (state: Map<_, _>) =
                if not <| state.IsEmpty then
                    let map =
                        (Map.empty, state)
                        ||> Map.fold (fun map channel trivia ->
                            let elapsedSeconds = utcNow() - trivia.Timestamp |> _.TotalSeconds |> int
                            let hintTimes = [ 15 ; 30 ]
                            let answerTime = 55
                            let hints = trivia.HintsSent

                            if elapsedSeconds = answerTime then
                                mb.Post (SendAnswer trivia.Channel)
                                map |> Map.add channel trivia
                            else if hintTimes |> List.contains elapsedSeconds && not <| (hints |> List.contains elapsedSeconds) then
                                mb.Post(SendHint trivia.Channel)
                                map |> Map.add channel { trivia with HintsSent = elapsedSeconds :: trivia.HintsSent }
                            else
                                map |> Map.add channel trivia
                        )

                    mb.Post Update
                    map
                else
                    state

            let sendQuestion channel state =
                async {
                    match state |> Map.tryFind channel with
                    | Some { Questions = q :: _ } ->
                        do! sendMessage channel $"[Trivia - %s{q.Category}] (Hints: {q.Hints.Length}) Question: %s{q.Question}"
                        return state
                    | _ -> return state
                }

            let sendHint channel state =
                async {
                    match state |> Map.tryFind channel with
                    | Some { Questions = q :: qs } ->
                        match q.Hints with
                        | h :: hs ->
                            do! sendMessage channel $"[Trivia] Hint: %s{h}"
                            let trivia = { state[channel] with Questions = { q with Hints = hs } :: qs }
                            return state |> Map.add channel trivia
                        | _ -> return state
                    | _ -> return state
                }

            let sendAnswer channel state =
                async {
                    match state |> Map.tryFind channel with
                    | Some { Questions = [ q ] } ->
                        do! sendMessage channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                        return state |> Map.remove channel
                    | Some { Questions = q :: qs } ->
                        do! sendMessage channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                        mb.Post (SendQuestion channel)
                        return state |> Map.add channel { state[channel] with Questions = qs }
                    | _ -> return state
                }

            let userMessaged channel username message state =
                async {
                    match state |> Map.tryFind channel with
                    | Some { Questions = q :: qs } when String.Compare(q.Answer, message, ignoreCase = true) = 0 ->
                        do! sendMessage channel $"""[Trivia] @%s{username}, got it! The answer was %s{q.Answer}"""

                        if qs.IsEmpty then
                            return state |> Map.remove channel
                        else
                            mb.Post (SendQuestion channel)
                            return state |> Map.add channel { state[channel] with Questions = qs }
                        | _ -> return state
                    | _ -> return state
                }

            let rec loop state =
                async {
                    let! msg = mb.Receive()

                    let! newState =
                        match msg with
                        | TriviaRequest.StartTrivia config -> startTrivia config state
                        | TriviaRequest.StopTrivia channel -> stopTrivia channel state
                        | TriviaRequest.SendQuestion channel -> sendQuestion channel state
                        | TriviaRequest.Update -> update state |> Async.create
                        | TriviaRequest.SendHint channel -> sendHint channel state
                        | TriviaRequest.SendAnswer channel -> sendAnswer channel state
                        | TriviaRequest.UserMessaged (channel, userId, username, message) -> userMessaged channel username message state

                    do! Async.Sleep(50)
                    return! loop newState
                }

            Logging.trace "Trivia agent started."
            loop (Map.empty)
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
                        do! twitchChatClient.SendAsync(Commands.JoinM (channels |> Seq.map snd))
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
                        do! twitchChatClient.SendAsync(Commands.Pong pong)
                    }

                let sendPrivateMessage channel message = twitchChatClient.SendAsync(Commands.PrivMsg(channel, message))

                let sendReplyMessage messageId channel message = twitchChatClient.SendAsync(Commands.ReplyMsg(messageId, channel, message))

                let sendRawIrcMessage message = twitchChatClient.SendAsync(Commands.Raw message)

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
                            do! twitchChatClient.SendAsync(Commands.Join channel)
                        | LeaveChannel channel -> do! twitchChatClient.SendAsync(Commands.Part channel)
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
        do! twitchChatClient.SendAsync(Commands.JoinM (channels |> Seq.map snd))
        do! services.EmoteService.RefreshGlobalEmotes(user.Id, accessToken)
        let refreshChannelEmotes = channels |> Seq.map fst |> Seq.map (fun c -> services.EmoteService.RefreshChannelEmotes c)
        do! refreshChannelEmotes |> Async.Parallel |> Async.Ignore

        chatAgent.Start()
        reminderAgent.Start()
        triviaAgent.Start()
    }
