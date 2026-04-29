namespace Chatbot.Commands

[<AutoOpen>]
module JoinChannel =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch
    open Chatbot.Database

    let joinChannel db (twitchService: TwitchService) context =

        asyncResult {
            let! channelName = context.MessageArgs |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.Users.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! _ = Channels.get db (user.Id |> int) |> AsyncResult.requireNone (InvalidArgs "Channel already added")

            match! Channels.add db (Models.NewChannel.create user.Id user.DisplayName) with
            | DatabaseResult.Failure -> return! internalError "Failed to add and join channel"
            | DatabaseResult.Success _ ->
                return [
                    CommandResponse.join user.DisplayName user.Id
                    Message $"Channel added (%s{user.DisplayName})"
                ]
        }
