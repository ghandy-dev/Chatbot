namespace Chatbot.Commands

[<AutoOpen>]
module Logs =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Common.Parsing
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain
    open Chatbot.Core.Services.Ivr

    let private mapHttpError = fun err ->
        match err with
        | 403 -> AsyncResult.ok "User/channel has opted out"
        | 404 -> AsyncResult.ok "No message(s) found"
        | _ -> AsyncResult.error err

    let randomLine (ivrService: IvrService) context =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel (channel, _) ->
                let! message =
                    match context.MessageArgs with
                    | [] -> ivrService.GetChannelRandomLine channel
                    | user :: _ -> ivrService.GetUserRandomLine channel user
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return [ Message message ]
        }

    let randomQuote (ivrService: IvrService) context =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel (channel, _) ->
                let! message =
                    ivrService.GetUserRandomLine channel context.Username
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return [ Message message ]
        }

    let searchKeys = [ "channel" ; "user" ; "reverse" ; "offset" ]

    let search (ivrService: IvrService) context =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel (channel, _) ->
                let kvp = KeyValueParser.parse context.MessageArgs searchKeys
                let channel = kvp.KeyValues.TryFind "channel" |> Option.defaultValue channel
                let user = kvp.KeyValues.TryFind "user" |> Option.defaultValue context.Username
                let reverse = kvp.KeyValues.TryFind "reverse" |> Option.bind tryParseBoolean |> Option.defaultValue false
                let offset = kvp.KeyValues.TryFind "offset" |> Option.bind tryParseInt |> Option.defaultValue 0
                let query = kvp.Input |> String.join " "

                let! message =
                    ivrService.Search channel user query reverse offset
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return [ Message message ]
        }

    let lastLine (ivrService: IvrService) context =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel (channel, _) ->
                let user = context.MessageArgs |> List.tryHead |> Option.defaultValue context.Username

                let! message =
                    ivrService.GetLastLine channel user
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return [ Message message ]
        }