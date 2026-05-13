namespace Chatbot.Commands

[<AutoOpen>]
module LeaveChannel =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain.Types
    open Chatbot.Core.Services.Twitch

    let leaveChannel (channels: IChannelRepository) (twitchService: TwitchService) context =
        asyncResult {
            let! channelName = context.MessageArgs |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.Users.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! channel = channels.Get (int user.Id) |> AsyncResult.requireSome (InvalidArgs $"Not in channel %s{channelName}")

            return!
                async {
                    match! channels.Delete (channel.ChannelId |> int) with
                    | Error _ -> return internalError "Failed to remove and leave channel"
                    | Ok _ ->
                        return Ok [
                            CommandResponse.leave channel.ChannelName
                            Message $"Channel removed (%d{channel.ChannelId} %s{channel.ChannelName})"
                        ]
                }
        }
