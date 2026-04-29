namespace Chatbot.Core.Services

open System

type AccessToken = {
    AccessToken: string
    ExpiresAt: DateTimeOffset
} with

    member this.hasExpired () =
        DateTimeOffset.UtcNow > this.ExpiresAt