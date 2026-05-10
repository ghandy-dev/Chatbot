namespace Chatbot.Commands

[<AutoOpen>]
module StreamThumbnail =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch

    let thumbnail (twitchService: TwitchService) context =
        asyncResult {
            let! channel =
                match context.MessageArgs with
                | [] -> invalidArgs "No channel specified"
                | channel :: _ -> Ok channel

            match! twitchService.Streams.GetStream channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch") with
            | None -> return [ Message "Channel not found or is not currently live" ]
            | Some stream ->
                let url = stream.ThumbnailUrl.Replace("{width}", "640").Replace("{height}", "360")
                return [ Message url ]
        }