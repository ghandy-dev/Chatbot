namespace Commands

[<AutoOpen>]
module Logs =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Commands
    open CommandError
    open Parsing

    let private ivrService = Services.ivrService

    let randomLine args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | [] ->
                    let! message =  ivrService.GetChannelRandomLine channel.Channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
                    return Message message
                | user :: _ ->
                    let! message =  ivrService.GetUserRandomLine channel.Channel user |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
                    return Message message
        }

    let randomQuote args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let! message =  ivrService.GetUserRandomLine channel.Channel context.Username |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
                return Message message
        }

    let searchKeys = [ "channel" ; "user" ; "reverse" ]

    let search args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let kvp = KeyValueParser.parse args searchKeys
                let channel = kvp.KeyValues.TryFind "channel" |> Option.defaultValue channel.Channel
                let user = kvp.KeyValues.TryFind "user" |> Option.defaultValue context.Username
                let reverse = kvp.KeyValues.TryFind "reverse" |> Option.bind tryParseBoolean |> Option.defaultValue false
                let query = kvp.Input |> strJoin " "

                let! message =
                    ivrService.Search channel user query reverse
                    |> AsyncResult.orElseWith (fun err ->
                        match err with
                        | 404 -> AsyncResult.ok "No message(s) found"
                        | _ -> AsyncResult.error err
                    )
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return Message message
        }

    let lastLine args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let user = args |> List.tryHead |> Option.defaultValue context.Username
                let! message = ivrService.GetLastLine channel.Channel user |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
                return Message message
        }