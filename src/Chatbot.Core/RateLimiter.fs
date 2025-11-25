module RateLimiter

module Rates =

    let [<Literal>] MessageLimit_Chat = 20 // per 30 seconds
    let [<Literal>] Interval_Chat = 30s

    let [<Literal>] MessageLimit_Whispers = 100
    let [<Literal>] Interval_Whispers = 60s

type RateLimiter(messagesPerInterval, interval: int16) =

    let interval = interval |> int
    let mutable messageCount = 0
    let mutable lastMessageTimestamp = utcNow()

    member private _.TimeSinceLastReset =
        (utcNow() - lastMessageTimestamp).TotalSeconds |> int

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
            lastMessageTimestamp <- utcNow()
            true
        elif this.MessageCount < messagesPerInterval then
            messageCount <- messageCount + 1
            lastMessageTimestamp <- utcNow()
            true
        else
            false