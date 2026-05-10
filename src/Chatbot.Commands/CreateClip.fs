namespace Chatbot.Commands

[<AutoOpen>]
module CreatClip =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch

    let createClip (twitchService: TwitchService) context =
        asyncResult {
            let! channel =
                match context.MessageArgs with
                | [] ->
                    match context.MessageSource with
                    | Whisper _ -> invalidArgs "No channel specified"
                    | Channel (channel, _) -> Ok channel
                | channel :: _ -> Ok channel

            let! accessToken = twitchService.Authentication.GetAccessToken () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Auth")
            let! userOpt = twitchService.Users.GetUser channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
            let! streamOpt = twitchService.Streams.GetStream channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Stream")

            match userOpt, streamOpt with
            | None, _ -> return [ Message "User not found" ]
            | _, None -> return [ Message "Channel is not currently live, nothing to clip" ]
            | Some user, Some _ ->
                let! clipOpt = twitchService.Clips.CreateClip user.Id accessToken.AccessToken |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Clips")

                return
                    match clipOpt with
                    | None -> [ Message "Unable to create clip" ]
                    | Some clip -> [ Message clip.EditUrl ]
        }