namespace Chatbot.Commands

[<AutoOpen>]
module Echo =

    let echo args = Ok <| Message $"""{String.concat " " args}"""
