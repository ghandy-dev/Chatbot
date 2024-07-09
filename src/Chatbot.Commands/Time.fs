namespace Chatbot.Commands

[<AutoOpen>]
module Time =

    open System

    [<Literal>]
    let private dateTimeFormat = "yyyy/MM/dd HH:mm:ss"

    let time () =
        Ok <| Message $"{DateTime.UtcNow.ToString(dateTimeFormat)} (UTC)"
