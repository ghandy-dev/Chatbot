namespace Chatbot.Commands

[<AutoOpen>]
module JoinChannel =

    open Chatbot
    open Chatbot.Database
    open Chatbot.Database.Types
    open Chatbot.HelixApi

    let joinChannel (args: string list) =
        async {
            match args with
            | [] -> return Error "No channel specified."
            | channel :: _ ->
                match! Users.getUser channel |+> TTVSharp.tryHeadResult "Channel not found" with
                | Error err -> return Error err
                | Ok user ->
                    match! ChannelRepository.getById (user.Id |> int) with
                    | Some _ -> return Ok <| Message "Channel already added"
                    | None ->
                        match! ChannelRepository.add (Channel.create user.Id user.DisplayName) with
                        | DatabaseResult.Success _ -> return Ok <| BotAction(JoinChannel channel, $"Channel added ({channel})")
                        | DatabaseResult.Failure -> return Error "Failed to add and join channel"

        }
