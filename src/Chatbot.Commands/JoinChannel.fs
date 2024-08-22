namespace Chatbot.Commands

[<AutoOpen>]
module JoinChannel =

    open Chatbot.Database
    open Chatbot.Database.Types.Channels

    open TTVSharp.Helix

    let joinChannel (args: string list) =
        async {
            match args with
            | [] -> return Error "No channel specified."
            | channel :: _ ->
                match! Users.getUser channel with
                | None -> return Error "User not found"
                | Some user ->
                    match! ChannelRepository.getById (user.Id |> int) with
                    | Some _ -> return Ok <| Message "Channel already added"
                    | None ->
                        match! ChannelRepository.add (Channel.create user.Id user.DisplayName) with
                        | DatabaseResult.Success _ -> return Ok <| BotAction(JoinChannel channel, $"Channel added ({channel})")
                        | DatabaseResult.Failure -> return Error "Failed to add and join channel"

        }
