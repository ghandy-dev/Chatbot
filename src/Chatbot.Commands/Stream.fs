namespace Commands

[<AutoOpen>]
module Stream =

    open System

    open Twitch.Helix

    let stream args =
        async {
            match args with
            | [] -> return Message "No channel specified."
            | channel :: _ ->
                match!
                    Users.getUser channel
                    |-> Result.fromOption "User not found"
                    |> Result.bindAsync (fun user -> Streams.getStream user.Id |-> Result.fromOption "Stream not live")
                with
                | Error err -> return Message err
                | Ok stream ->
                    let viewerCount = stream.ViewerCount.ToString("N0")
                    let uptime = (DateTimeOffset.UtcNow - stream.StartedAt).ToString("hh\h\:mm\m\:ss\s")
                    let url = $"https://twitch.tv/{stream.UserLogin}"

                    return Message $"{stream.UserName} is streaming {stream.GameName} for {viewerCount} viewers. \"{stream.Title}\" {url} [{uptime}]"
        }
