namespace Commands

[<AutoOpen>]
module Channel =

    let twitchService = Services.services.TwitchService

    let channel args =
        async {
            match args with
            | [] -> return Message "No channel specified."
            | channel :: _ ->
                match!
                    twitchService.GetUser channel |-> Result.fromOption "User not found"
                    |> Result.bindAsync (fun user -> twitchService.GetChannel user.Id |-> Result.fromOption "Channel not found")
                with
                | Error err -> return Message err
                | Ok channel ->
                    let broadcaster = channel.BroadcasterName
                    let title = channel.Title
                    let game = channel.GameName

                    return Message $"\"{title}\" Game: {game} https://twitch.tv/{broadcaster} "
        }
