namespace Chatbot.Commands

[<AutoOpen>]
module StreamThumbnail =

    open FsToolkit.ErrorHandling

    open Chatbot.Core
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch
    open Chatbot.Core.Services.ImageUpload
    open Chatbot.Core.Types

    let thumbnail env (twitchService: TwitchService) (imageUploadService: IImageUploadService) context =
        let httpClient = env.HttpClient

        asyncResult {
            let! channel =
                match context.MessageArgs with
                | [] -> invalidArgs "No channel specified"
                | channel :: _ -> Ok channel

            match! twitchService.Streams.GetStream channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch") with
            | None -> return [ Message "Channel not found or is not currently live" ]
            | Some stream ->
                let thumbnail = stream.ThumbnailUrl.Replace("{width}", "640").Replace("{height}", "360")

                let request = Http.Request.get thumbnail
                let! response = Http.send httpClient request

                let! bytes =
                    response
                    |> Http.Response.toResult
                    |> Result.eitherMap _.Bytes _.StatusCode
                    |> Result.mapError (CommandHttpError.fromHttpStatusCode "nuuls")

                let! url = imageUploadService.Upload bytes |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "nuuls")

                return [ Message url ]
        }