namespace Chatbot.Commands

[<AutoOpen>]
module Channel =

    open TTVSharp.Helix

    let channel args =
        async {
            match args with
            | [] -> return Message "No channel specified."
            | channel :: _ ->
                match!
                    Users.getUser channel |-> Result.fromOption "User not found"
                    |> Result.bindAsync (fun user -> Channels.getChannel user.Id |-> Result.fromOption "Channel not found")
                with
                | Error err -> return Message err
                | Ok channel ->
                    let broadcaster = channel.BroadcasterName
                    let title = channel.Title
                    let game = channel.GameName

                    return Message $"\"{title}\" Game: {game} https://twitch.tv/{broadcaster} "
        }
