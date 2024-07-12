namespace Chatbot.Commands

[<AutoOpen>]
module LeaveChannel =

    open Chatbot
    open Chatbot.Database
    open TTVSharp.Helix

    let leaveChannel (args: string list) =
        async {
            match args with
            | [] -> return Error "No channel specified"
            | channel :: _ ->
                match! Users.getUser channel with
                | None -> return Error "User not found"
                | Some user ->
                    match! ChannelRepository.getById (user.Id |> int) with
                    | None -> return Ok <| Message $"Not in channel ({user.Id} {user.DisplayName})"
                    | Some _ ->
                        match! ChannelRepository.delete (user.Id |> int) with
                        | DatabaseResult.Success _ -> return Ok <| BotAction (LeaveChannel channel, $"removed channel ({user.Id} {user.DisplayName})")
                        | DatabaseResult.Failure -> return Error $"Failed to delete and leave channel"
        }
