namespace Chatbot.Commands

[<AutoOpen>]
module AddBetween =

    let addBetween args =
        match args with
        | [ _ ] -> Ok <| Message "No text provided."
        | word :: text ->
            [
                yield word
                for t in text -> $"{t} {word}"
                yield word
            ]
            |> String.concat " "
            |> Message
            |> Ok
        | [] -> Ok <| Message "No input/text provided"
