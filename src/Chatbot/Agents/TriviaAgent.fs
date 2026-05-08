module Chatbot.Agents.Trivia

open System

open Microsoft.Extensions.Logging

open Chatbot.Common
open Chatbot.Core.IRC
open Chatbot.Core.Domain
open Chatbot.Core.Types

type TriviaMessage =
    | StartTrivia of config: Trivia
    | StopTrivia of channel: string
    | SendQuestion of channel: string
    | SendHint of channel: string
    | SendAnswer of channel: string
    | Update
    | TwitchEvent of TwitchEvent

let create env (twitchChatClient: Chatbot.Twitch.TwitchClient) cancellationToken =
    new MailboxProcessor<TriviaMessage>(
        (fun mb ->
            let logger = env.Logger

            let send channel message = twitchChatClient.Send(Request.privMsg channel message)

            let startTrivia (trivia: Trivia) state =
                async {
                    match state |> Map.containsKey trivia.Channel with
                    | true ->
                        do send trivia.Channel "Trivia already started"
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
                        do send channel "Trivia stopped"
                        return state |> Map.remove channel
                    | false -> return state
                }

            let update state =
                if not <| (state |> Map.isEmpty) then
                    let state' =
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
                    state'
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
                            do send channel $"%d{config.Count+1 - config.Questions.Length}/%d{config.Count} [Trivia - %s{q.Category}] (Hints: {q.Hints.Length}) Question: %s{q.Question}"
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
                            do send channel $"[Trivia] Hint: %s{h}"
                            let trivia = { state[channel] with Questions = { q with Hints = hs } :: qs }
                            return state |> Map.add channel trivia
                        | _ -> return state
                    | _ -> return state
                }

            let sendAnswer channel state =
                async {
                    match state |> Map.tryFind channel with
                    | Some { Questions = [ q ] } ->
                        do send channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                        return state |> Map.remove channel
                    | Some { Questions = q :: qs } ->
                        do send channel $"[Trivia] No one got it. The answer was: %s{q.Answer}"
                        mb.Post (SendQuestion channel)
                        return state |> Map.add channel { state[channel] with Questions = qs }
                    | _ -> return state
                }

            let userMessaged channel username message state =
                async {
                    match state |> Map.tryFind channel with
                    | Some { Questions = q :: qs } when message |> strCompareIgnoreCase q.Answer ->
                        do send channel $"""[Trivia] @%s{username}, got it! The answer was %s{q.Answer}"""

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

                    let! state' =
                        match msg with
                        | StartTrivia config -> startTrivia config state
                        | StopTrivia channel -> stopTrivia channel state
                        | SendQuestion channel -> sendQuestion channel state
                        | Update -> update state |> async.Return
                        | SendHint channel -> sendHint channel state
                        | SendAnswer channel -> sendAnswer channel state
                        | TwitchEvent (ChannelMessage message) -> userMessaged message.Channel message.Username message.Message state
                        | TwitchEvent (ChannelReplyMessage message) -> userMessaged message.Channel message.Username message.Message state
                        | _ -> state |> async.Return

                    do! Async.Sleep(100)
                    return! loop state'
                }

            logger.LogInformation("Trivia agent started.")
            loop (Map.empty)
        ),
        cancellationToken
    )