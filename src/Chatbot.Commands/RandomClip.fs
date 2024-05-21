namespace Chatbot.Commands

[<AutoOpen>]
module RandomClip =

    open Chatbot
    open Chatbot.HelixApi
    open TTVSharp.Helix

    let private getClipsResult (user: TTVSharp.Helix.User) =
        async {
            match!
                helixApi.Clips.GetClipsAsync(new GetClipsRequestByBroadcasterId(BroadcasterId = user.Id, First = 50)) |> Async.AwaitTask
                |+-> TTVSharp.toResult
            with
            | Error error -> return Error error
            | Ok response ->
                match response.Data |> Seq.toList with
                | [] -> return Error "No clips found."
                | clips -> return Ok clips[System.Random.Shared.Next(clips.Length)]
        }

    let randomClip (args: string list) (context: Context) =
        async {
            match context.Source with
            | Whisper _ -> return Error $"This command can only be executed from within the context of a channel"
            | Channel channel ->

            let channel =
                match args with
                | [] -> channel
                | channel :: _ -> channel

            match! Users.getUser channel |+-> TTVSharp.tryHeadResult "User not found." |> AsyncResult.bind getClipsResult with
            | Ok clip -> return Ok <| Message $"\"{clip.Title}\" ({clip.ViewCount} views, {clip.CreatedAt.ToShortDateString()}) {clip.Url}"
            | Error error -> return Error error
        }
