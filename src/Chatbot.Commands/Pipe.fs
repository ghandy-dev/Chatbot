namespace Chatbot.Commands

[<AutoOpen>]
module Pipe =

    let pipe args =
        let recombined = String.concat " " args
        let commands = recombined.Split("|") |> List.ofArray

        match commands with
        | []
        | [ _ ] -> Error "At least 2 commands must be piped together"
        | cs -> Ok <| Pipe cs
