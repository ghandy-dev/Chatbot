namespace Chatbot.Commands

[<AutoOpen>]
module LeaveChannel =

    open Chatbot
    open Chatbot.Database
    open Chatbot.HelixApi

    let leaveChannel (args: string list) =
        async {
            match args with
            | [] -> return Error "No channel specified."
            | channel :: _ ->
                match! Users.getUser channel |+-> TTVSharp.tryHeadResult "User not found." with
                | Error err -> return Error err
                | Ok user ->
                    match! ChannelRepository.getById (user.Id |> int) with
                    | None -> return Error $"Not in channel ({user.Id} {user.DisplayName})"
                    | Some _ ->
                        match! ChannelRepository.delete (user.Id |> int) with
                        | DatabaseResult.Success _ -> return Ok <| BotAction (LeaveChannel channel, $"removed channel ({user.Id} {user.DisplayName})")
                        | DatabaseResult.Failure ex -> return Error $"Failed to delete channel: {ex.Message}"
        }
