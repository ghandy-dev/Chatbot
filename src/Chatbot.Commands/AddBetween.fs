namespace Commands

[<AutoOpen>]
module AddBetween =

    let addBetween args =
        match args with
        | [] -> Error <| InvalidArgs "No input provided"
        | word :: text ->
            [ yield word
              for t in text -> $"{t} {word}"
            ]
            |> String.concat " "
            |> Message
            |> Ok
