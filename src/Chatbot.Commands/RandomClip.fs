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
                |+> TTVSharp.toResult
            with
            | Error err -> return Error err
            | Ok response -> return Ok(response.Data |> Seq.toList)
        }

    let private getUserResult username =
        async {
            return!
                helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = [ username ])) |> Async.AwaitTask
                |+> TTVSharp.tryHeadResult "User not found"
        }

    let private getChannel args (context: Context) =
        match context.Source with
        | Whisper _ ->
            match args with
            | channel :: _ -> Ok channel
            | _ -> Error "This command can only be executed from within the context of a channel"
        | Channel channel ->
            match args with
            | [] -> Ok channel
            | channel :: _ -> Ok channel

    let randomClip (args: string list) (context: Context) =
        async {
            match! Async.create (getChannel args context)
                |> AsyncResult.bind getUserResult
                |> AsyncResult.bind getClipsResult with
                | Ok clip ->
                    match clip with
                    | [] -> return Ok <| Message "No clips found"
                    | clips ->
                        let clip = clips[System.Random.Shared.Next(clips.Length)]
                        return Ok <| Message $""""{clip.Title}" ({clip.ViewCount.ToString("N0")} views, {clip.CreatedAt.ToShortDateString()}) {clip.Url}"""
                | Error error -> return Error error
        }
