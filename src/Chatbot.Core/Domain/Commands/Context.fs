namespace Chatbot.Core.Domain.Commands

open Chatbot.Core.Domain

type Context = {
    UserId: string
    Username: string
    MessageArgs: string list
    MessageSource: MessageSource
    Emotes: Emotes
    MessageEmotes: Map<string, string>
}

module Context =

    let create userId username args source emotes messageEmotes = {
        UserId = userId
        Username = username
        MessageArgs = args
        MessageSource = source
        Emotes = emotes
        MessageEmotes = messageEmotes
    }

