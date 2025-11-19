namespace Commands

[<AutoOpen>]
module Logs =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Commands
    open CommandError
    open Parsing

    let private ivrService = Services.ivrService

    let private mapHttpError = fun err ->
        match err with
        | 403 -> AsyncResult.ok "User/channel has opted out"
        | 404 -> AsyncResult.ok "No message(s) found"
        | _ -> AsyncResult.error err

    let randomLine context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let! message =
                    match context.Args with
                    | [] -> ivrService.GetChannelRandomLine channel.Channel
                    | user :: _ -> ivrService.GetUserRandomLine channel.Channel user
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return Message message
        }

    let randomQuote context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let! message =
                    ivrService.GetUserRandomLine channel.Channel context.Username
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return Message message
        }

    let searchKeys = [ "channel" ; "user" ; "reverse" ; "offset" ]

    let search context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let kvp = KeyValueParser.parse context.Args searchKeys
                let channel = kvp.KeyValues.TryFind "channel" |> Option.defaultValue channel.Channel
                let user = kvp.KeyValues.TryFind "user" |> Option.defaultValue context.Username
                let reverse = kvp.KeyValues.TryFind "reverse" |> Option.bind tryParseBoolean |> Option.defaultValue false
                let offset = kvp.KeyValues.TryFind "offset" |> Option.bind tryParseInt |> Option.defaultValue 0
                let query = kvp.Input |> strJoin " "

                let! message =
                    ivrService.Search channel user query reverse offset
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return Message message
        }

    let lastLine context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let user = context.Args |> List.tryHead |> Option.defaultValue context.Username

                let! message =
                    ivrService.GetLastLine channel.Channel user
                    |> AsyncResult.orElseWith mapHttpError
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return Message message
        }