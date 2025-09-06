namespace Commands

[<AutoOpen>]
module Pick =

    open System
    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open CommandError

    let pick context =
        result {
            match context.Args with
            | [] -> return! invalidArgs "No items provided"
            | head :: tail ->
                let delimiterPattern = @"^delimiter:(.+)$"
                let m = Regex.Match(head, delimiterPattern)

                let items =
                    match m.Success with
                    | false -> context.Args
                    | true ->
                        String.concat " " tail
                        |> _.Split(m.Groups[1].Value, StringSplitOptions.TrimEntries)
                        |> List.ofArray

                return Message $"{items |> List.randomChoice}"
        }
