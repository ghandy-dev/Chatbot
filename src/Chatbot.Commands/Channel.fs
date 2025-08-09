namespace Commands

[<AutoOpen>]
module Channel =

    open FsToolkit.ErrorHandling

    let twitchService = Services.services.TwitchService

    let channel args =
        asyncResult {
            let! channelName = args |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! channel =
                twitchService.GetChannel user.Id
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Channel")
                |> AsyncResult.bindRequireSome (InvalidArgs "Channel not found")

            let url = $"https://twitch.tv/{channel.BroadcasterName}"
            let title = channel.Title
            let game = channel.GameName

            return Message $"\"{title}\" Game: {game} {url}"
        }
