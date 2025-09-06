namespace Commands

[<AutoOpen>]
module AddBetween =

    open FsToolkit.ErrorHandling

    open CommandError

    let addBetween context =
        result {
            match context.Args with
            | [] -> return! invalidArgs "No input provided"
            | word :: text ->
                return
                    seq { yield word ; for t in text -> $"{t} {word}" }
                    |> String.concat " "
                    |> Message
        }
