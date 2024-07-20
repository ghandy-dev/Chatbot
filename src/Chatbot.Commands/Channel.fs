namespace Chatbot.Commands

[<AutoOpen>]
module Channel =

    open TTVSharp.Helix

    let channel args =
        async {
            match args with
            | [] -> return Error "No channel specified."
            | channel :: _ ->
                match!
                    Users.getUser channel |-> Result.fromOption "User not found"
                    |> Result.bindAsync (fun user -> Channels.getChannel user.Id |-> Result.fromOption "Channel not found")
                with
                | Error e -> return Error e
                | Ok channel ->
                    let broadcaster = channel.BroadcasterName
                    let title = channel.Title
                    let game = channel.GameName

                    return Ok <| Message $"\"{title}\" Game: {game} https://twitch.tv/{broadcaster} "
        }
