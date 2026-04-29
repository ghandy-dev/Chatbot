namespace Chatbot.Commands

[<AutoOpen>]
module Pick =

    open System
    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError

    let pick context =
        result {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No items provided"
            | head :: tail ->
                let delimiterPattern = @"^delimiter:(.+)$"
                let m = Regex.Match(head, delimiterPattern)

                let items =
                    match m.Success with
                    | false -> context.MessageArgs
                    | true ->
                        String.concat " " tail
                        |> _.Split(m.Groups[1].Value, StringSplitOptions.TrimEntries)
                        |> List.ofArray

                return [ Message $"{items |> List.randomChoice}" ]
        }
