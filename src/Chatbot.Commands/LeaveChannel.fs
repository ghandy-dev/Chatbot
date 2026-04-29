namespace Chatbot.Commands

[<AutoOpen>]
module LeaveChannel =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch
    open Chatbot.Database

    let leaveChannel db (twitchService: TwitchService) context =
        asyncResult {
            let! channelName = context.MessageArgs |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.Users.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! channel = Channels.get db (int user.Id) |> AsyncResult.requireSome (InvalidArgs $"Not in channel %s{channelName}")

            match! Channels.delete db (channel.ChannelId |> int) with
            | DatabaseResult.Failure -> return! internalError "Failed to remove and leave channel"
            | DatabaseResult.Success _ ->
                return [
                    CommandResponse.leave channel.ChannelName
                    Message $"Channel removed (%s{channel.ChannelId} %s{channel.ChannelName})"
                ]
        }
