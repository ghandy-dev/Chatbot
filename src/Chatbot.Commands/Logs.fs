namespace Commands

[<AutoOpen>]
module Logs =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Commands
    open CommandError

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

    let search args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel channel ->
                let query = args |> strJoin " "

                let! message =
                    ivrService.Search channel.Channel context.Username query
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