module Twitch

open Configuration

open TTVSharp
open TTVSharp.Helix

let toResult (response: IApiResponse<'a>) =
    match response.StatusCode with
    | code when code >= 200 && code < 300 -> Ok response.Body
    | _ -> Error response.Error.Message

let private logError r =
    match r with
    | Error err -> Logging.error (sprintf "Twitch API error: %s" err) (new exn())
    | Ok _ -> ()

    r

module Helix =

    open Microsoft.Extensions.Options

    let private selectHelix = fun response -> response |> Result.bind (fun r -> Ok (r :> HelixResponse<_>).Data)
    let private tryGetData (response: IApiResponse<'a>) = toResult response |> selectHelix  |> logError |> Result.toOption
    let private tryHead response = tryGetData response |> Option.bind Seq.tryHead

    let private options =
        Options.Create<HelixApiOptions>(new HelixApiOptions(ClientId = appConfig.Twitch.ClientId, ClientSecret = appConfig.Twitch.ClientSecret))

    let helixApi = new HelixApi(options)

    module Channels =

        let getChannel userId =
            helixApi.Channels.GetChannelAsync(new GetChannelRequest(BroadcasterId = userId)) |> Async.AwaitTask
            |-> tryHead

    module Chat =

        let getUserChatColor userId =
            helixApi.Chat.GetUserChatColorAsync(new GetUserChatColorRequest(UserIds = [ userId ])) |> Async.AwaitTask
            |-> tryHead

    module Clips =

        let getClips userId (dateFrom: System.DateTime) (dateTo: System.DateTime) =
            helixApi.Clips.GetClipsAsync(new GetClipsRequestByBroadcasterId(BroadcasterId = userId, StartedAt = dateFrom, EndedAt = dateTo, First = 50)) |> Async.AwaitTask
            |-> tryGetData

    module Emotes =

        let getGlobalEmotes () =
            helixApi.Chat.GetGlobalEmotesAsync() |> Async.AwaitTask
            |-> tryGetData

        let getChannelEmotes channelId =
            helixApi.Chat.GetChannelEmotesAsync(new GetChannelEmotesRequest(BroadcasterId = channelId)) |> Async.AwaitTask
            |-> tryGetData

        let getEmoteSet emoteSetId =
            helixApi.Chat.GetEmoteSetsAsync(new GetEmoteSetsRequest(EmoteSetIds = [ emoteSetId ])) |> Async.AwaitTask
            |-> tryGetData

        let getEmoteSets emoteSetIds =
            helixApi.Chat.GetEmoteSetsAsync(new GetEmoteSetsRequest(EmoteSetIds = emoteSetIds)) |> Async.AwaitTask
            |-> tryGetData

        let getUserEmotes userId accessToken =
            async {
                let rec pageRequest emotes cursor =
                    async {
                        let! r = helixApi.Chat.GetUserEmotesAsync(new GetUserEmotesRequest(UserId = userId, After = cursor), accessToken) |> Async.AwaitTask
                        let data = r |> tryGetData

                        match data with
                        | Some d ->
                            if r.Body.Pagination.HasNextPage then return! pageRequest (d |> Seq.append emotes) r.Body.Pagination.Cursor
                            else return d |> Seq.append emotes |> Seq.toList :> System.Collections.Generic.IReadOnlyList<UserEmote>
                        | _ -> return emotes |> Seq.toList :> System.Collections.Generic.IReadOnlyList<UserEmote>
                    }

                return! pageRequest Seq.empty "" |-> Some
            }

    module Streams =

        let getStreams (first: int) =
            helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(First = first)) |> Async.AwaitTask
            |-> tryGetData

        let getStream userId =
            helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(UserIds = [ userId ])) |> Async.AwaitTask
            |-> tryHead

    module Users =

        let getUser username =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = [ username ])) |> Async.AwaitTask
            |-> tryHead

        let getUsersByUsername usernames =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = usernames)) |> Async.AwaitTask
            |-> tryGetData

        let getUsersById userIds =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Ids = userIds)) |> Async.AwaitTask
            |-> tryGetData

        let getAccessTokenUser accessToken =
            helixApi.Users.GetUsersAsync(accessToken) |> Async.AwaitTask
            |-> tryHead

    module Videos =

        let getLatestVod userId =
            helixApi.Videos.GetVideosByUserIdAsync(new GetVideosByUserIdRequest(UserIds = [ userId ], First = 1)) |> Async.AwaitTask
            |-> tryHead

    module Whispers =

        let sendWhisper fromUserId toUserId message accessToken =
            helixApi.Whispers.SendWhisperAsync(
                new SendWhisperRequest(FromUserId = fromUserId, ToUserId = toUserId, Message = message),
                accessToken = accessToken
            )
            |> Async.AwaitTask
