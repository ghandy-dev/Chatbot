namespace Chatbot.Commands

[<AutoOpen>]
module Channel =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch

    let channel (twitchService: TwitchService) context =
        asyncResult {
            let! channelName = context.MessageArgs |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.Users.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! channel =
                twitchService.Channels.GetChannel user.Id
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Channel")
                |> AsyncResult.bindRequireSome (InvalidArgs "Channel not found")

            let url = $"https://twitch.tv/{channel.BroadcasterName}"
            let title = channel.Title
            let game = channel.GameName

            return [ Message $"\"{title}\" Game: {game} {url}" ]
        }
