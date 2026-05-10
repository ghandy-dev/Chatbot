namespace Chatbot.Commands

[<AutoOpen>]
module ProfilePicture =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch

    let profilePicture (twitchService: TwitchService) context =
        asyncResult {
            let username =
                match context.MessageArgs with
                | [] -> context.Username
                | username :: _ -> username

            match! twitchService.Users.GetUser username |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch") with
            | None -> return [ Message "User not found" ]
            | Some user -> return [ Message user.ProfileImageUrl ]
        }