namespace Chatbot.Commands

[<AutoOpen>]
module Vod =

    open Chatbot
    open Chatbot.HelixApi
    open TTVSharp.Helix

    let private latestVod =
        fun (user: User) ->
            async {
                return!
                    helixApi.Videos.GetVideosByUserIdAsync(new GetVideosByUserIdRequest(UserIds = [ user.Id ], First = 1))
                    |> Async.AwaitTask
                    |+> TTVSharp.tryHeadResult "No vods found."
            }

    let vod args =
        async {
            match args with
            | [] -> return Error "No channel specified"
            | channel :: _ ->
                match! Users.getUser channel |+> TTVSharp.tryHeadResult "User not found." |> AsyncResult.bind latestVod with
                | Ok video -> return Ok <| Message $"\"{video.Title}\" {video.CreatedAt.ToShortDateString()} {video.Url} [{video.Duration}]"
                | Error err -> return Error err
        }
