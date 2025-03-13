namespace Commands

[<AutoOpen>]
module JoinChannel =

    open Database
    open Database.Types.Channels

    let twitchService = Services.services.TwitchService

    let joinChannel (args: string list) =
        async {
            match!
                args |> Async.create |-> List.tryHead |-> Result.fromOption "No channel specified"
                |> Result.bindAsync (fun c -> twitchService.GetUser c |-> Result.fromOption "User not found")
            with
            | Error err -> return Message err
            | Ok user ->
                match! ChannelRepository.getById (user.Id |> int) with
                | Some _ -> return Message "Channel already added"
                | None ->
                    match! ChannelRepository.add (Channel.create user.Id user.DisplayName) with
                    | DatabaseResult.Success _ -> return BotAction(JoinChannel (user.DisplayName, user.Id), Some $"Channel added (%s{user.DisplayName})")
                    | DatabaseResult.Failure -> return Message "Failed to add and join channel"
        }
