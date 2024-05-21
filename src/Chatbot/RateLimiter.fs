module Chatbot.MessageLimiter

open System

[<Literal>]
let MessagesPerMinute_Chat = 40

[<Literal>]
let MessagesPerMinute_Whispers = 100

type RateLimiter(messagesPerMinute: int) =
    let maxMessagesPerMinute = messagesPerMinute
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


    member this.TimeUntilReset = this.TimeSinceLastReset - 60

    member this.CanSend () =
        if this.TimeSinceLastReset > 60 then
            messageCount <- 1
            lastMessageTimestamp <- DateTime.Now
            true
        elif this.MessageCount < maxMessagesPerMinute then
            messageCount <- messageCount + 1
            lastMessageTimestamp <- DateTime.Now
            true
        else
            false
