namespace Chatbot.Core.Domain

open Types

type User =
    {
        UserId: UserId
        Username: string
        IsAdmin: bool
    }

    static member create userId username =
        {
            UserId = userId
            Username = username
            IsAdmin = false
        }

type NewUser =
    {
        UserId: UserId
        Username: string
        IsAdmin: bool
    }

    static member create userId username =
        {
            UserId = userId
            Username = username
            IsAdmin = false
        }
