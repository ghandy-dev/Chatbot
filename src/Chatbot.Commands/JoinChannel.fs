namespace Chatbot.Commands

[<AutoOpen>]
module JoinChannel =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Types
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch

    let joinChannel (channels: IChannelRepository) (twitchService: TwitchService) context =
        asyncResult {
            let! channelName = context.MessageArgs |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.Users.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! _ = channels.Get (user.Id |> int) |> AsyncResult.requireNone (InvalidArgs "Channel already added")

            return!
                async {
                    let newChannel = NewChannel.create (int user.Id) user.DisplayName

                    match! channels.Add newChannel with
                    | Error _ -> return internalError "Failed to add and join channel"
                    | Ok _ ->
                        return Ok [
                            CommandResponse.join user.DisplayName user.Id
                            Message $"Channel added (%s{user.DisplayName})"
                        ]
                }
        }
