namespace Chatbot.Commands

[<AutoOpen>]
module Pipe =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError

    let pipe pipeSeparator context =
        result {
            let recombined = context.MessageArgs |> strJoin " "
            let commands = recombined |> strSplit pipeSeparator |> List.ofArray

            match commands with
            | []
            | [ _ ] -> return! invalidArgs "At least 2 commands must be piped together"
            | cs -> return [ Message "TODO" ]
        }
