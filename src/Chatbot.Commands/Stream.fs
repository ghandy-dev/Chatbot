namespace Chatbot.Commands

[<AutoOpen>]
module Stream =

    open System

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch

    let stream (twitchService: TwitchService) context =

        asyncResult {
            let! channel = context.MessageArgs |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")
            let! user =
                twitchService.Users.GetUser channel
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            match! twitchService.Streams.GetStream user.Id |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Stream") with
            | None -> return [ Message $"%s{channel} is not live at the moment" ]
            | Some stream ->
                let viewerCount = stream.ViewerCount.ToString("N0")
                let uptime = (DateTimeOffset.UtcNow - stream.StartedAt).ToString("hh\h\:mm\m\:ss\s")
                let url = $"https://twitch.tv/{stream.UserLogin}"

                return [ Message $"{stream.UserName} is streaming {stream.GameName} for {viewerCount} viewers. \"{stream.Title}\" {url} [{uptime}]" ]
        }
