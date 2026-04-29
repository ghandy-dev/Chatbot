module Chatbot.RateLimiter

open System.Collections.Generic

open FSharpPlus

open Chatbot.Common

let [<Literal>] GlobalSlow = 1000

let [<Literal>] MessageLimit_Chat = 20 // per 30 seconds
let [<Literal>] ResetInterval_Chat = 30s

let [<Literal>] MessageLimit_Whispers = 100
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

    let channels = new Dictionary<string, RateLimitState>()

    member _.CanSend channel =
        let now = epochTime ()

        match channels |> Dict.tryGetValue channel with
        | Some rates when now > rates.ResetAt && now - rates.LastMessageTimestamp > int64 GlobalSlow + 200L ->
            channels[channel] <- RateLimitState.create (epochTime() + int64 interval)
            true
        | Some rates when rates.MessageCount < messageLimit && now - rates.LastMessageTimestamp > int64 GlobalSlow + 200L ->
            channels[channel] <- { rates with MessageCount = rates.MessageCount + 1 }
            true
        | Some rates ->
            false
        | None ->
            channels[channel] <- RateLimitState.create (epochTime() + int64 interval)
            true