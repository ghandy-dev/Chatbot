module Twitch

open FSharpPlus
open FsToolkit.ErrorHandling
open TTVSharp
open TTVSharp.Helix

open Configuration

let toResult (response: IApiResponse<'a>) =
    match response.StatusCode with
    | code when code >= 200 && code < 300 -> Ok response.Body
    | _ -> Error response

let private logError (error: IApiResponse<'a>) = Logging.errorEx ($"Twitch API error: %d{error.StatusCode} %s{error.Error.Message}") exn

module Helix =

    open Microsoft.Extensions.Options

    let private selectHelix response = (response :> HelixResponse<_>).Data |> List.ofSeq

    let private handleResponse response =
        response
        |> toResult
        |> Result.teeError logError
        |> Result.mapError _.StatusCode
        |> Result.map selectHelix

    let private options =
        Options.Create<HelixApiOptions>(new HelixApiOptions(ClientId = appConfig.Twitch.ClientId, ClientSecret = appConfig.Twitch.ClientSecret))

    let helixApi = new HelixApi(options)

    module Channels =

        let getChannel userId =
            helixApi.Channels.GetChannelAsync(new GetChannelRequest(BroadcasterId = userId)) |> Async.AwaitTask
            |> Async.map handleResponse
            |> AsyncResult.map List.tryHead

    module Chat =

        let getUserChatColor userId =
            helixApi.Chat.GetUserChatColorAsync(new GetUserChatColorRequest(UserIds = [ userId ])) |> Async.AwaitTask
            |> Async.map handleResponse
            |> AsyncResult.map List.tryHead

    module Clips =

        let getClips userId (dateFrom: System.DateTime) (dateTo: System.DateTime) =
            helixApi.Clips.GetClipsAsync(new GetClipsRequestByBroadcasterId(BroadcasterId = userId, StartedAt = dateFrom, EndedAt = dateTo, First = 50)) |> Async.AwaitTask
            |> Async.map handleResponse

    module Emotes =

        let getGlobalEmotes () =
            helixApi.Chat.GetGlobalEmotesAsync() |> Async.AwaitTask
            |> Async.map handleResponse

        let getChannelEmotes channelId =
            helixApi.Chat.GetChannelEmotesAsync(new GetChannelEmotesRequest(BroadcasterId = channelId)) |> Async.AwaitTask
            |> Async.map handleResponse

        let getEmoteSet emoteSetId =
            helixApi.Chat.GetEmoteSetsAsync(new GetEmoteSetsRequest(EmoteSetIds = [ emoteSetId ])) |> Async.AwaitTask
            |> Async.map handleResponse

        let getEmoteSets emoteSetIds =
            helixApi.Chat.GetEmoteSetsAsync(new GetEmoteSetsRequest(EmoteSetIds = emoteSetIds)) |> Async.AwaitTask
            |> Async.map handleResponse

        let getUserEmotes userId accessToken =
            async {
                let rec pageRequest acc cursor =
                    async {
                        let! r = helixApi.Chat.GetUserEmotesAsync(new GetUserEmotesRequest(UserId = userId, After = cursor), accessToken) |> Async.AwaitTask

                        match r |> handleResponse with
                        | Ok emotes ->
                            match emotes with
                            | [] -> return Ok (acc |> List.ofSeq)
                            | es ->
                                if r.Body.Pagination.HasNextPage then
                                    return! pageRequest (es |> Seq.append acc) r.Body.Pagination.Cursor
                                else
                                    return Ok (es|> Seq.append acc |> List.ofSeq)

                        | Error err -> return Error err
                    }

                return! pageRequest Seq.empty ""
            }

    module Streams =

        let getStreams (first: int) =
            helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(First = first)) |> Async.AwaitTask
            |> Async.map handleResponse

        let getStream userId =
            helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(UserIds = [ userId ])) |> Async.AwaitTask
            |> Async.map handleResponse
            |> AsyncResult.map List.tryHead

    module Users =

        let getUser username =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = [ username ])) |> Async.AwaitTask
            |> Async.map handleResponse
            |> AsyncResult.map List.tryHead

        let getUsersByUsername usernames =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = usernames)) |> Async.AwaitTask
            |> Async.map handleResponse

        let getUsersById userIds =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Ids = userIds)) |> Async.AwaitTask
            |> Async.map handleResponse

        let getAccessTokenUser accessToken =
            helixApi.Users.GetUsersAsync(accessToken) |> Async.AwaitTask
            |> Async.map handleResponse
            |> AsyncResult.map List.tryHead

    module Videos =

        let getLatestVod userId =
            helixApi.Videos.GetVideosByUserIdAsync(new GetVideosByUserIdRequest(UserIds = [ userId ], First = 1)) |> Async.AwaitTask
            |> Async.map handleResponse
            |> AsyncResult.map List.tryHead

    module Whispers =

        let sendWhisper fromUserId toUserId message accessToken =
            helixApi.Whispers.SendWhisperAsync(
                new SendWhisperRequest(FromUserId = fromUserId, ToUserId = toUserId, Message = message),
                accessToken = accessToken
            )
            |> Async.AwaitTask
