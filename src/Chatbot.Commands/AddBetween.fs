namespace Chatbot.Commands

[<AutoOpen>]
module AddBetween =

    let addBetween args =
        match args with
        | [] -> "No input provided"
        | word :: text -> [ yield word ; for t in text -> $"{t} {word}" ] |> String.concat " "
        |> Message
