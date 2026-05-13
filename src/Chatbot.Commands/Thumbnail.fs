namespace Chatbot.Commands

[<AutoOpen>]
module StreamThumbnail =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch
    open Chatbot.Core.Types

    let thumbnail env (twitchService: TwitchService) context =
        let httpClient = env.HttpClient

        asyncResult {
            let! channel =
                match context.MessageArgs with
                | [] -> invalidArgs "No channel specified"
                | channel :: _ -> Ok channel

            match! twitchService.Streams.GetStream channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch") with
            | None -> return [ Message "Channel not found or is not currently live" ]
            | Some stream ->
                let thumbnailUrl = stream.ThumbnailUrl.Replace("{width}", "0").Replace("{height}", "0")

                return [ Message thumbnailUrl ]
        }