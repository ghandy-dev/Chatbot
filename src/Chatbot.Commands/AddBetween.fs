namespace Chatbot.Commands

[<AutoOpen>]
module AddBetween =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError

    let addBetween context =
        result {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No input provided"
            | word :: text ->
                let message =
                    seq { yield word ; for t in text -> $"{t} {word}" }
                    |> String.join " "

                return [ Message message ]
        }
