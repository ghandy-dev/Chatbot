module TTVSharp

open TTVSharp.Helix

module Helpers =

    let toResult (response: TTVSharp.IApiResponse<'a>) =
        match response.StatusCode with
        | code when code >= 200 && code < 300 -> Ok response.Body
        | _ -> Error response.Error.Message

    module Helix =

        let private toResult' (response) = toResult response

        let tryGetData (helixResponse) =
            match toResult' helixResponse with
            | Error _ -> None
            | Ok response -> Some (response :> HelixResponse<'a>).Data

        let tryHead (helixResponse) =
            match tryGetData helixResponse with
            | None -> None
            | Some data -> data |> Array.ofSeq |> Array.tryHead

module Helix =

    open Chatbot.Configuration
    open Helpers

    open Microsoft.Extensions.Options

    let private options =
        Options.Create<HelixApiOptions>(new HelixApiOptions(ClientId = Twitch.config.ClientId, ClientSecret = Twitch.config.ClientSecret))

    let helixApi = new HelixApi(options)

    module Chat =
        let getUserChatColor userId =
            helixApi.Chat.GetUserChatColorAsync(new GetUserChatColorRequest(UserIds = [ userId ])) |> Async.AwaitTask
            |+> Helix.tryHead

    module Clips =

        let getClips userId (dateFrom: System.DateTime) (dateTo: System.DateTime) =
            helixApi.Clips.GetClipsAsync(new GetClipsRequestByBroadcasterId(BroadcasterId = userId, StartedAt = dateFrom, EndedAt = dateTo, First = 50)) |> Async.AwaitTask
            |+> Helix.tryGetData

    module Streams =

        let getStreams (first: int) =
            helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(First = first)) |> Async.AwaitTask
            |+> Helix.tryGetData

        let getStream userId =
            helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(UserIds = [ userId ])) |> Async.AwaitTask
            |+> Helix.tryHead

    module Users =

        let getUser username =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = [ username ])) |> Async.AwaitTask
            |+> Helix.tryHead

    module Videos =

        let getLatestVod userId =
            helixApi.Videos.GetVideosByUserIdAsync(new GetVideosByUserIdRequest(UserIds = [ userId ], First = 1)) |> Async.AwaitTask
            |+> Helix.tryHead

    module Whispers =

        let sendWhisper fromUserId toUserId message accessToken =
            helixApi.Whispers.SendWhisperAsync(
                new SendWhisperRequest(FromUserId = fromUserId, ToUserId = toUserId, Message = message),
                accessToken = accessToken
            )
            |> Async.AwaitTask
