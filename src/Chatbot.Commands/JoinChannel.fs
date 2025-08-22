namespace Commands

[<AutoOpen>]
module JoinChannel =

    open FsToolkit.ErrorHandling

    open CommandError
    open Database

    let twitchService = Services.services.TwitchService

    let joinChannel (args: string list) =
        asyncResult {
            let! channelName = args |> List.tryHead |> Result.requireSome (InvalidArgs "No channel specified")

            let! user =
                twitchService.GetUser channelName
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! _ = ChannelRepository.get (user.Id |> int) |> AsyncResult.requireNone (InvalidArgs "Channel already added")

            match! ChannelRepository.add (Models.NewChannel.create user.Id user.DisplayName) with
            | DatabaseResult.Failure -> return! internalError "Failed to add and join channel"
            | DatabaseResult.Success _ -> return BotCommand.joinChannel user.DisplayName user.Id $"Channel added (%s{user.DisplayName})"
        }
