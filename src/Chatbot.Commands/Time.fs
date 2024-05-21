namespace Chatbot.Commands

[<AutoOpen>]
module Time =

    open System

    [<Literal>]
    let private dateTimeFormat = "dd/MM/yyyy HH:mm:ss"

    let time () =
        Ok <| Message $"{DateTime.UtcNow.ToString(dateTimeFormat)} (UTC)"
