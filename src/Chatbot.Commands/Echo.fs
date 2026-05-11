namespace Chatbot.Commands

[<AutoOpen>]
module Echo =

    open Chatbot.Core.Domain.Commands

    let echo context = Ok [ Message $"""{String.join " " context.MessageArgs}""" ]
