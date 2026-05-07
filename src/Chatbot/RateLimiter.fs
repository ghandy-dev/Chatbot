module Chatbot.RateLimiter

open System.Collections.Generic

open FSharpPlus

open Chatbot.Common

let [<Literal>] GlobalSlow = 1000

let [<Literal>] MessageLimit_Chat = 20 // per 30 seconds
let [<Literal>] ResetInterval_Chat = 30s

let [<Literal>] MessageLimit_Whispers = 100 // per 60 seconds
let [<Literal>] ResetInterval_Whispers = 60s

type RateLimitState = {
    MessageCount: int
    ResetAt: int64
    LastMessageTimestamp: int64
}

module RateLimitState =

    let create resetAt = {
        MessageCount = 1
        ResetAt = resetAt
        LastMessageTimestamp = epochTime ()
    }

type RateLimiter(messageLimit, interval: int16) =

    let cache = new Dictionary<string, RateLimitState>()

    member _.CanSend key =
        let now = epochTime ()

        match cache |> Dict.tryGetValue key with
        | Some rates when now > rates.ResetAt && now - rates.LastMessageTimestamp > int64 GlobalSlow + 200L ->
            cache[key] <- RateLimitState.create (epochTime() + int64 interval)
            true
        | Some rates when rates.MessageCount < messageLimit && now - rates.LastMessageTimestamp > int64 GlobalSlow + 200L ->
            cache[key] <- { rates with MessageCount = rates.MessageCount + 1 }
            true
        | Some rates ->
            false
        | None ->
            cache[key] <- RateLimitState.create (epochTime() + int64 interval)
            true