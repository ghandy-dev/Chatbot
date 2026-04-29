namespace Chatbot.Commands

[<AutoOpen>]
module NameColor =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch

    let namecolor (twitchService: TwitchService) context =
        asyncResult {
            let! user =
                context.MessageArgs |> List.tryHead |? context.Username
                |> twitchService.Users.GetUser
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            match! twitchService.Chat.GetUserChatColor user.Id |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - UserChatColor") with
            | None -> return [ Message "User not found" ]
            | Some userColor -> return [ Message $"{userColor.UserName} {userColor.Color}" ]
        }
