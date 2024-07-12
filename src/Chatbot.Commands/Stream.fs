namespace Chatbot.Commands

[<AutoOpen>]
module Stream =

    open System

    open TTVSharp.Helix

    let stream args =
        async {
            match args with
            | [] -> return Error "No channel specified."
            | channel :: _ ->
                match!
                    Users.getUser channel
                    |> AsyncResult.fromOption "User not found"
                    |> AsyncResult.bind (fun user -> Streams.getStream user.Id |> AsyncResult.fromOption "Stream not live")
                with
                | Error e -> return Error e
                | Ok stream ->
                    let viewerCount = stream.ViewerCount.ToString("N0")
                    let uptime = (stream.StartedAt - DateTime.UtcNow).ToString("hh\\h:mm\\m:ss\\s")
                    let url = $"https://twitch.tv/{stream.UserLogin}"

                    return
                        Ok
                        <| Message
                            $"{stream.UserName} is streaming {stream.GameName} for {viewerCount} viewers. \"{stream.Title}\" {url} [{uptime}]"
        }
