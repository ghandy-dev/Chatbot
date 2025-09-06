namespace Commands

[<AutoOpen>]
module Pipe =

    open FsToolkit.ErrorHandling

    open CommandError

    let pipeSeperator = "|"

    let pipe context =
        result {
            let recombined = String.concat " " context.Args
            let commands = recombined.Split(pipeSeperator) |> List.ofArray

            match commands with
            | []
            | [ _ ] -> return! invalidArgs "At least 2 commands must be piped together"
            | cs -> return Pipe cs
        }
