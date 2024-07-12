namespace Chatbot.Commands

[<AutoOpen>]
module Vod =

    open TTVSharp.Helix

    let vod args =
        async {
            match args with
            | [] -> return Error "No channel specified"
            | channel :: _ ->
                match!
                    Users.getUser channel
                    |> AsyncResult.fromOption "User not found"
                    |> AsyncResult.bind (fun user -> Videos.getLatestVod user.Id |> AsyncResult.fromOption "No VOD found")
                with
                | Error err -> return Error err
                | Ok video -> return Ok <| Message $"\"{video.Title}\" {video.CreatedAt.ToShortDateString()} {video.Url} [{video.Duration}]"
        }
