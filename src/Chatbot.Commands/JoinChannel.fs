namespace Commands

[<AutoOpen>]
module JoinChannel =

    open Database

    let twitchService = Services.services.TwitchService

    let joinChannel (args: string list) =
        async {
            match!
                args |> Async.create |-> List.tryHead |-> Result.fromOption "No channel specified"
                |> Result.bindAsync (fun c -> twitchService.GetUser c |-> Result.fromOption "User not found")
            with
            | Error err -> return Message err
            | Ok user ->
                match! ChannelRepository.get (user.Id |> int) with
                | Some _ -> return Message "Channel already added"
                | None ->
                    match! ChannelRepository.add (Models.NewChannel.create user.Id user.DisplayName) with
                    | DatabaseResult.Success _ -> return BotAction(JoinChannel (user.DisplayName, user.Id), Some $"Channel added (%s{user.DisplayName})")
                    | DatabaseResult.Failure -> return Message "Failed to add and join channel"
        }
