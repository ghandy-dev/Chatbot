namespace Commands

[<AutoOpen>]
module Vod =

    open FsToolkit.ErrorHandling

    open CommandError

    let twitchService = Services.services.TwitchService

    let vod context =
        asyncResult {
            match context.Args with
            | [] -> return! invalidArgs "No channel specified"
            | channel :: _ ->
                let! user =
                    twitchService.GetUser channel
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                    |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

                match! twitchService.GetLatestVod user.Id |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Video") with
                | None -> return Message "No VODs found"
                | Some vod -> return Message $""""{vod.Title}" {vod.CreatedAt.ToString(Utils.DateStringFormat)} {vod.Url} [{vod.Duration}]"""
        }
