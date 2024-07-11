namespace Chatbot.Commands

[<AutoOpen>]
module Stream =

    open System

    open Chatbot
    open Chatbot.HelixApi
    open TTVSharp.Helix

    let private innerStream =
        fun (user: User) ->
            async {
                return!
                    helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(UserIds = [ user.Id ])) |> Async.AwaitTask
                    |+> TTVSharp.tryHeadResult $"{user.DisplayName} is not currently live."
            }

    let stream args =
        async {
            match args with
            | [] -> return Error "No channel specified."
            | channel :: _ ->
                match! Users.getUser channel |+> TTVSharp.tryHeadResult "Channel not found." |> AsyncResult.bind innerStream with
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
