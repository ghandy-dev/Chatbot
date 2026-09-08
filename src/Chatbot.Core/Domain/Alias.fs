namespace Chatbot.Core.Domain

open Types

type Alias =
    {
        Name: string
        Command: string
    }

type NewAlias =
    {
        UserId: UserId
        Name: string
        Command: string
    }

    static member create userId name command =
        {
            UserId = userId
            Name = name
            Command = command
        }

type UpdateAlias =
    {
        UserId: UserId
        Name: string
        Command: string
    }

    static member create userId name command =
        {
            UserId = userId
            Name = name
            Command = command
        }
