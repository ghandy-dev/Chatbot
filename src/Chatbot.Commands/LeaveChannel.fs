namespace Commands

[<AutoOpen>]
module LeaveChannel =

    open Database
    open Twitch.Helix

    let leaveChannel (args: string list) =
        async {
            match!
                args |> Async.create |-> List.tryHead |-> Result.fromOption "No channel specified"
                |> Result.bindAsync (fun c -> Users.getUser c |-> Result.fromOption "User not found")
                |> Result.bindAsync (fun u -> ChannelRepository.getById (u.Id |> int) |-> Result.fromOption $"Not in channel {u.DisplayName}")
                |> Result.bindAsync (fun u ->  ChannelRepository.getById (u.ChannelId |> int) |-> Result.fromOption $"Not in channel {u.ChannelName}")
            with
            | Error err -> return Message err
            | Ok channel ->
                match! ChannelRepository.delete (channel.ChannelId |> int) with
                | DatabaseResult.Success _ -> return BotAction (LeaveChannel channel.ChannelName, $"removed channel ({channel.ChannelId} {channel.ChannelName})")
                | DatabaseResult.Failure -> return Message $"Failed to delete and leave channel"
        }
