namespace Commands

[<AutoOpen>]
module LeaveChannel =

    open Database

    let twitchService = Services.services.TwitchService

    let leaveChannel (args: string list) =
        async {
            match!
                args |> Async.create |-> List.tryHead |-> Result.fromOption "No channel specified"
                |> Result.bindAsync (fun c -> twitchService.GetUser c |-> Result.fromOption "User not found")
                |> Result.bindAsync (fun u -> ChannelRepository.get (int u.Id) |-> Result.fromOption $"Not in channel {u.DisplayName}")
                |> Result.bindAsync (fun u ->  ChannelRepository.get (int u.ChannelId) |-> Result.fromOption $"Not in channel {u.ChannelName}")
            with
            | Error err -> return Message err
            | Ok channel ->
                match! ChannelRepository.delete (channel.ChannelId |> int) with
                | DatabaseResult.Success _ -> return BotAction (LeaveChannel channel.ChannelName, Some $"removed channel (%s{channel.ChannelId} %s{channel.ChannelName})")
                | DatabaseResult.Failure -> return Message $"Failed to delete and leave channel"
        }
