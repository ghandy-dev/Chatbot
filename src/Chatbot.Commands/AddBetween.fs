namespace Chatbot.Commands

[<AutoOpen>]
module AddBetween =

    let addBetween args =
        match args with
        | [] -> Error "No input provided"
        | [ _ ] -> Error "No text provided"
        | word :: text ->
            [
                yield word
                for t in text -> $"{t} {word}"
            ]
            |> String.concat " "
            |> Message
            |> Ok
