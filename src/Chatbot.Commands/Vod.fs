namespace Chatbot.Commands

[<AutoOpen>]
module Vod =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch

    let vod (twitchService: TwitchService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No channel specified"
            | channel :: _ ->
                let! user =
                    twitchService.Users.GetUser channel
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                    |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

                match! twitchService.Videos.GetLatestVod user.Id |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Video") with
                | None -> return [ Message "No VODs found" ]
                | Some vod -> return [ Message $""""{vod.Title}" {vod.CreatedAt.ToString(Utils.DateStringFormat)} {vod.Url} [{vod.Duration}]""" ]
        }
