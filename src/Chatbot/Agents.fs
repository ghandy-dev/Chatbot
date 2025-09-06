module Agents

open System

open Authorization
open Clients
open Commands
open Database
open IRC
open MessageHandlers
open Services
open Types

module ReminderAgent =

    let create (twitchChatClient: TwitchClient) cancellationToken =
        new MailboxProcessor<ReminderMessage>(
            (fun mb ->
                let checkReminders () =
                    async {
                        let! reminders = ReminderRepository.getTimedReminders ()

                        for reminder in reminders do
                            let ts = DateTime.UtcNow - reminder.Timestamp
                            let sender = if reminder.FromUsername = reminder.TargetUsername then "yourself" else $"@%s{reminder.FromUsername}"
                            let message = $"@%s{reminder.TargetUsername}, reminder from %s{sender} (%s{formatTimeSpan ts} ago): %s{reminder.Message}"
                            do twitchChatClient.Send(Request.privMsg reminder.Channel message)

                        do! Async.Sleep(250)
                        mb.Post CheckReminders
                    }

                let userMessaged channel userId username =
                    async {
                        match! ReminderRepository.getPendingReminderCount userId with
                        | DatabaseResult.Success c when c > 0 ->
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
                                        |> strJoin ", "

                                    if rs |> Seq.length = 1 then
                                        $"reminder from %s{sender} %s{message}"
                                    else
                                        $"reminders from %s{sender} %s{message}"
                                )
                                |> strJoin ", "

                            if message.Length > 500 then
                                match! services.PastebinService.CreatePaste "" message with
                                | Error _ -> Logging.errorEx "Failed to create paste" exn
                                | Ok url -> do twitchChatClient.Send(Request.privMsg channel $"@%s{username}, reminders were too long to send, check %s{url} for your reminders")
                            else
                                do twitchChatClient.Send(Request.privMsg channel $"@%s{username}, %s{message}")
                        | _ -> ()
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

module TriviaAgent =

    let create (twitchChatClient: TwitchClient) cancellationToken =
        new MailboxProcessor<TriviaRequest>(
            (fun mb ->
                let sendMessage channel message = twitchChatClient.Send(Request.privMsg channel message)

                let startTrivia (trivia: TriviaConfig) state =
                    async {
                        match state |> Map.containsKey trivia.Channel with
                        | true ->
                            do sendMessage trivia.Channel "Trivia already started"
                            return state
                        | false ->
                            mb.Post (SendQuestion trivia.Channel)
                            mb.Post Update
                            return state |> Map.add trivia.Channel trivia
                    }

                let stopTrivia channel state =
                    async {
                        match state |> Map.containsKey channel with
                        | true ->
                            do sendMessage channel "Trivia stopped"
                            return state |> Map.remove channel
                        | false -> return state
                    }

                let update state =
                    if not <| (state |> Map.isEmpty) then
                        let map =
                            (Map.empty, state)
                            ||> Map.fold (fun map channel trivia ->
                                let elapsedSeconds = utcNow() - trivia.Timestamp |> _.TotalSeconds |> int
                                let hintTimes = [ 15 ; 30 ]
                                let answerTime = 55
                                let hints = trivia.HintsSent

                                if elapsedSeconds = answerTime then
                                    mb.Post (SendAnswer trivia.Channel)
                                    map |> Map.add channel { trivia with Timestamp = DateTime.MaxValue }
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
                        let resetHints state = { state with HintsSent = [] }
                        let setStartTimestamp state = { state with Timestamp = utcNow() }

                        match state |> Map.tryFind channel with
                        | Some config ->
                            match config.Questions with
                            | q :: _ ->
                                do sendMessage channel $"%d{config.Count+1 - config.Questions.Length}/%d{config.Count} [Trivia - %s{q.Category}] (Hints: {q.Hints.Length}) Question: %s{q.Question}"
                                let state = state |> Map.add channel (config |> resetHints |> setStartTimestamp)
                                return state
                            | [] -> return state
                        | None -> return state
                    }

                let sendHint channel state =
                    async {
                        match state |> Map.tryFind channel with
                        | Some { Questions = q :: qs } ->
                            match q.Hints with
                            | h :: hs ->
                                do sendMessage channel $"[Trivia] Hint: %s{h}"
                                let trivia = { state[channel] with Questions = { q with Hints = hs } :: qs }
                                return state |> Map.add channel trivia
                            | _ -> return state
                        | _ -> return state
                    }

                let sendAnswer channel state =
                    async {
                        match state |> Map.tryFind channel with
                        | Some { Questions = [ q ] } ->
                            do sendMessage channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                            return state |> Map.remove channel
                        | Some { Questions = q :: qs } ->
                            do sendMessage channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                            mb.Post (SendQuestion channel)
                            return state |> Map.add channel { state[channel] with Questions = qs }
                        | _ -> return state
                    }

                let userMessaged channel username message state =
                    async {
                        match state |> Map.tryFind channel with
                        | Some { Questions = q :: qs } when message |> strCompareIgnoreCase q.Answer ->
                            do sendMessage channel $"""[Trivia] @%s{username}, got it! The answer was %s{q.Answer}"""

                            if qs.IsEmpty then
                                return state |> Map.remove channel
                            else
                                mb.Post (SendQuestion channel)
                                return state |> Map.add channel { state[channel] with Questions = qs }
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
                            | TriviaRequest.Update -> update state |> async.Return
                            | TriviaRequest.SendHint channel -> sendHint channel state
                            | TriviaRequest.SendAnswer channel -> sendAnswer channel state
                            | TriviaRequest.UserMessaged (channel, userId, username, message) -> userMessaged channel username message state

                        do! Async.Sleep(100)
                        return! loop newState
                    }

                Logging.trace "Trivia agent started."
                loop (Map.empty)
            ),
            cancellationToken
        )

module ChatAgent =

    let create (twitchChatClient: TwitchClient) (user: TTVSharp.Helix.User) (triviaAgent: MailboxProcessor<TriviaRequest>) cancellationToken =
        new MailboxProcessor<ClientRequest>(
            (fun mb ->
                let sendPrivateMessage channel message = twitchChatClient.Send(Request.privMsg channel message)
                let sendReplyMessage messageId channel message = twitchChatClient.Send(Request.replyMsg messageId channel message)
                let sendRawIrcMessage message = twitchChatClient.Send(Request.raw message)

                let sendWhisper userId message =
                    async {
                        match! tokenStore.GetToken TokenType.Twitch with
                        | Error _ -> Logging.info "Unable to send whisper. Couldn't retrieve access token for Twitch API."
                        | Ok token -> do twitchChatClient.SendWhisper userId message token
                    }

                let handleBotCommand command =
                    async {
                        match command with
                        | JoinChannel (channel, channelId) ->
                            do! services.EmoteService.RefreshChannelEmotes channelId
                            do twitchChatClient.Send(Request.join channel)
                        | LeaveChannel channel -> do twitchChatClient.Send(Request.part channel)
                        | RefreshGlobalEmotes provider ->
                            match! tokenStore.GetToken TokenType.Twitch with
                            | Error _ -> ()
                            | Ok token -> do! services.EmoteService.RefreshGlobalEmotes(user.Id, token)
                        | RefreshChannelEmotes channelId -> do! services.EmoteService.RefreshChannelEmotes(channelId)
                        | BotCommand.StartTrivia trivia -> do triviaAgent.Post (TriviaRequest.StartTrivia trivia)
                        | BotCommand.StopTrivia channel -> do triviaAgent.Post (TriviaRequest.StopTrivia channel)
                    }

                let rec loop () =
                    async {
                        match! mb.Receive() with
                        | ClientRequest.HandleIrcMessage message -> do! handleIrcMessage message mb
                        | ClientRequest.SendPrivateMessage(channel, message) -> do sendPrivateMessage channel message
                        | ClientRequest.SendWhisperMessage(userId, _, message) -> do! sendWhisper userId message
                        | ClientRequest.SendReplyMessage(messageId, channel, message) -> do sendReplyMessage messageId channel message
                        | ClientRequest.SendRawIrcMessage message -> do sendRawIrcMessage message
                        | ClientRequest.BotCommand command -> do! handleBotCommand command

                        return! loop ()
                    }

                loop ()
            ),
            cancellationToken
        )
