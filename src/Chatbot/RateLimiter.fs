module Chatbot.MessageLimiter

open System

module Rates =

    [<Literal>]
    let MessageLimit_Chat = 20 // per 30 seconds
    [<Literal>]
    let Interval_Chat = 30s

    [<Literal>]
    let MessageLimit_Whispers = 100
    [<Literal>]
    let Interval_Whispers = 60s


type RateLimiter(messagesPerInterval, interval: int16) =

    let interval = interval |> int
    let mutable messageCount = 0
    let mutable lastMessageTimestamp = DateTime.UtcNow

    member private _.TimeSinceLastReset =
        (DateTime.UtcNow - lastMessageTimestamp).TotalSeconds |> int

    member _.MessageCount
        with get () = messageCount
        and set (value) = messageCount <- value

    member _.LastReset
        with get () = lastMessageTimestamp
        and set (value) = lastMessageTimestamp <- value


    member this.TimeUntilReset = this.TimeSinceLastReset - interval

    member this.CanSend () =
        if this.TimeSinceLastReset > interval then
            messageCount <- 1
            lastMessageTimestamp <- DateTime.Now
            true
        elif this.MessageCount < messagesPerInterval then
            messageCount <- messageCount + 1
            lastMessageTimestamp <- DateTime.Now
            true
        else
            false
