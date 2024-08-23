namespace Chatbot.Commands

[<AutoOpen>]
module Echo =

    let echo args = Message $"""{String.concat " " args}"""
