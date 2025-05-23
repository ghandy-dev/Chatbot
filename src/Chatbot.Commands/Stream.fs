namespace Commands

[<AutoOpen>]
module Stream =

    open System

    let twitchService = Services.services.TwitchService

    let stream args =
        async {
            match args with
            | [] -> return Message "No channel specified."
            | channel :: _ ->
                match!
                    twitchService.GetUser channel
                    |-> Result.fromOption "User not found"
                    |> Result.bindAsync (fun user -> twitchService.GetStream user.Id |-> Result.fromOption "Stream not live")
                with
                | Error err -> return Message err
                | Ok stream ->
                    let viewerCount = stream.ViewerCount.ToString("N0")
                    let uptime = (DateTimeOffset.UtcNow - stream.StartedAt).ToString("hh\h\:mm\m\:ss\s")
                    let url = $"https://twitch.tv/{stream.UserLogin}"

                    return Message $"{stream.UserName} is streaming {stream.GameName} for {viewerCount} viewers. \"{stream.Title}\" {url} [{uptime}]"
        }
