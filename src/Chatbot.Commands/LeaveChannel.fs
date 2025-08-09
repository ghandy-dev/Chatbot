namespace Commands

[<AutoOpen>]
module LeaveChannel =

    open FsToolkit.ErrorHandling

    open CommandError
    open Database

    let twitchService = Services.services.TwitchService

    let leaveChannel (args: string list) =
        asyncResult {
            let! channelName = args |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! channel =
                ChannelRepository.get (int user.Id)
                |> AsyncResult.requireSome (InvalidArgs $"Not in channel %s{channelName}")

            match! ChannelRepository.delete (channel.ChannelId |> int) with
            | DatabaseResult.Failure -> return! internalError "Failed to remove and leave channel"
            | DatabaseResult.Success _ -> return BotAction (LeaveChannel channel.ChannelName, Some $"removed channel (%s{channel.ChannelId} %s{channel.ChannelName})")
        }
