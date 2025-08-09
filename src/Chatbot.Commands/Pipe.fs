namespace Commands

[<AutoOpen>]
module Pipe =

    let pipeSeperator = "|"

    let pipe args =
        let recombined = String.concat " " args
        let commands = recombined.Split(pipeSeperator) |> List.ofArray

        match commands with
        | []
        | [ _ ] -> Error <| InvalidArgs "At least 2 commands must be piped together"
        | cs -> Ok <| Pipe cs
