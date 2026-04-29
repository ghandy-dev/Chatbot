namespace Chatbot.Commands

[<AutoOpen>]
module UserId =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch

    let userId (twitchService: TwitchService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return [ Message context.UserId ]
            | username :: _ ->
                match! twitchService.Users.GetUser username |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch") with
                | None -> return [ Message "User not found" ]
                | Some user -> return [ Message user.Id ]
        }
