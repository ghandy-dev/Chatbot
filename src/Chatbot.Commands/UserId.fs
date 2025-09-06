namespace Commands

[<AutoOpen>]
module UserId =

    open FsToolkit.ErrorHandling

    let twitchService = Services.services.TwitchService

    let userId context =
        asyncResult {
            match context.Args with
            | [] -> return Message context.UserId
            | username :: _ ->
                match! twitchService.GetUser username |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch") with
                | None -> return Message "User not found"
                | Some user -> return Message user.Id
        }
