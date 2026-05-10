namespace Chatbot.Core.Services

module Twitch =

    open Microsoft.Extensions.Options

    open FsToolkit.ErrorHandling

    open TTVSharp
    open TTVSharp.Auth
    open TTVSharp.Helix

    open Chatbot.Core
    open Chatbot.Core.Caching
    open Chatbot.Core.Types

    type Authentication =
        abstract member GetAccessToken: unit -> Async<Result<AccessToken, int>>

    type Channels =
        abstract member GetChannel: channel: string -> Async<Result<Channel option, int>>

    type Chat =
        abstract member GetUserChatColor: userId: string -> Async<Result<UserChatColor option, int>>

    type Clips =
        abstract member GetClips: channel: string -> dateFrom: System.DateTime -> dateTo: System.DateTime -> Async<Result<Clip list, int>>

    type Emotes =
        abstract member GetGlobalEmotes: unit -> Async<Result<GlobalEmote list, int>>
        abstract member GetChannelEmotes: channel: string -> Async<Result<ChannelEmote list, int>>
        abstract member GetEmoteSet: emoteSetId: string -> Async<Result<EmoteSet list, int>>
        abstract member GetEmoteSets: emoteSetIds: string seq -> Async<Result<EmoteSet list, int>>
        abstract member GetUserEmotes: userId: string -> accessToken: string -> Async<Result<UserEmote list, int>>

    type Streams =
        abstract member GetStreams: count: int -> Async<Result<Stream list, int>>
        abstract member GetStream: channel: string -> Async<Result<Stream option, int>>

    type Users =
        abstract member GetUser: username: string -> Async<Result<User option, int>>
        abstract member GetUsersByUsername: usernames: string seq -> Async<Result<User list, int>>
        abstract member GetUsersById: userIds: string seq -> Async<Result<User list, int>>
        abstract member GetAccessTokenUser: accessToken: string -> Async<Result<User, int>>

    type Videos =
        abstract member GetLatestVod: userId: string -> Async<Result<Video option, int>>

    type Whispers =
        abstract member SendWhisper: fromUserId: string -> toUserId: string -> message: string -> accessToken: string -> Async<Result<int, int>>

    type TwitchService =
        abstract member Authentication: Authentication
        abstract member Channels: Channels
        abstract member Chat: Chat
        abstract member Clips: Clips
        abstract member Emotes: Emotes
        abstract member Streams: Streams
        abstract member Users: Users
        abstract member Videos: Videos
        abstract member Whispers: Whispers

    type TwitchOptions = {
        ClientId: string
        ClientSecret: string
        RefreshToken: string
    }

    let toResult (response: IApiResponse<'a>) =
        match response.StatusCode with
        | code when code >= 200 && code < 300 -> Ok response.Body
        | _ -> Error response

    let selectHelix response = (response :> HelixResponse<_>).Data |> List.ofSeq

    let handleResponse response =
        response
        |> toResult
        |> Result.mapError _.StatusCode
        |> Result.map selectHelix

    module Authentication =

        let create env (oauthClient: OAuthClient) config =

            let cache = env.Cache
            let refreshToken = config.RefreshToken
            let clientId = config.ClientId
            let clientSecret = config.ClientSecret

            let getAccessToken () =
                async {

                    match cache |> MemoryCache.tryGetValue<AccessToken> "twitch_accessToken" with
                    | Some token when not <| token.hasExpired () -> return Ok token
                    | _ ->
                        let! response = oauthClient.RefreshTokenAsync(clientId, clientSecret, refreshToken) |> Async.AwaitTask

                        if response.Error <> null then
                            return Error response.Error.Status
                        else
                            let token = { AccessToken = response.Data.AccessToken ; ExpiresAt = response.Data.ExpiresAt }
                            cache |> MemoryCache.set "twitch_accessToken" token |> ignore
                            return Ok token
                }

            {
                new Authentication with
                    member _.GetAccessToken () = getAccessToken ()
            }

    module Channels =

        let create (helixApi: HelixApi) =

            let getChannel userId =
                helixApi.Channels.GetChannelAsync(new GetChannelRequest(BroadcasterId = userId)) |> Async.AwaitTask
                |> Async.map handleResponse
                |> AsyncResult.map List.tryHead

            {
                new Channels with
                    member _.GetChannel channel = getChannel channel
            }

    module Chat =

        let create (helixApi: HelixApi) =

            let getUserChatColor userId =
                helixApi.Chat.GetUserChatColorAsync(new GetUserChatColorRequest(UserIds = [ userId ])) |> Async.AwaitTask
                |> Async.map handleResponse
                |> AsyncResult.map List.tryHead

            {
                new Chat with
                    member _.GetUserChatColor userId = getUserChatColor userId
            }

    module Clips =

        let create (helixApi: HelixApi) =

            let getClips userId (dateFrom: System.DateTime) (dateTo: System.DateTime) =
                helixApi.Clips.GetClipsAsync(new GetClipsRequestByBroadcasterId(BroadcasterId = userId, StartedAt = dateFrom, EndedAt = dateTo, First = 50)) |> Async.AwaitTask
                |> Async.map handleResponse

            {
                new Clips with
                    member _.GetClips channel dateFrom dateTo = getClips channel dateFrom dateTo
            }

    module Emotes =

        let create (helixApi: HelixApi) =

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

            {
                new Emotes with
                    member _.GetGlobalEmotes () = getGlobalEmotes ()
                    member _.GetChannelEmotes channel = getChannelEmotes channel
                    member _.GetEmoteSet emoteSetId = getEmoteSet emoteSetId
                    member _.GetEmoteSets emoteSetIds = getEmoteSets emoteSetIds
                    member _.GetUserEmotes userId accessToken = getUserEmotes userId accessToken
            }

    module Streams =

        let create (helixApi: HelixApi) =

            let getStreams (first: int) =
                helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(First = first)) |> Async.AwaitTask
                |> Async.map handleResponse

            let getStream channel =
                helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(LoginNames = [ channel ])) |> Async.AwaitTask
                |> Async.map handleResponse
                |> AsyncResult.map List.tryHead

            {
                new Streams with
                    member _.GetStreams count = getStreams count
                    member _.GetStream channel = getStream channel
            }

    module Users =

        let create (helixApi: HelixApi) =

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
                |> AsyncResult.map List.head

            {
                new Users with
                    member _.GetUser username = getUser username
                    member _.GetUsersByUsername usernames = getUsersByUsername usernames
                    member _.GetUsersById userIds = getUsersById userIds
                    member _.GetAccessTokenUser accessToken = getAccessTokenUser accessToken
            }

    module Videos =

        let create (helixApi: HelixApi) =

            let getLatestVod userId =
                helixApi.Videos.GetVideosByUserIdAsync(new GetVideosByUserIdRequest(UserIds = [ userId ], First = 1)) |> Async.AwaitTask
                |> Async.map handleResponse
                |> AsyncResult.map List.tryHead

            {
                new Videos with
                    member _.GetLatestVod userId = getLatestVod userId
            }

    module Whispers =

        let create (helixApi: HelixApi) =

            let sendWhisper fromUserId toUserId message accessToken =
                helixApi.Whispers.SendWhisperAsync(
                    new SendWhisperRequest(FromUserId = fromUserId, ToUserId = toUserId, Message = message),
                    accessToken = accessToken
                )
                |> Async.AwaitTask
                |> Async.map (Http.HttpStatusCode.toResult)

            {
                new Whispers with
                    member _.SendWhisper fromUserId toUserId message accessToken = sendWhisper fromUserId toUserId message accessToken
            }

    module TwitchService =

        let create env config =
            let options = Options.Create<HelixApiOptions>(new HelixApiOptions(ClientId = config.ClientId, ClientSecret = config.ClientSecret))

            let helixApi = new HelixApi(options)
            let oauthClient = new OAuthClient()

            {
                new TwitchService with
                    member _.Authentication with get () = Authentication.create env oauthClient config
                    member _.Channels with get () = Channels.create helixApi
                    member _.Chat with get () = Chat.create helixApi
                    member _.Clips with get () = Clips.create helixApi
                    member _.Emotes with get () = Emotes.create helixApi
                    member _.Streams with get () = Streams.create helixApi
                    member _.Users with get () = Users.create helixApi
                    member _.Videos with get () = Videos.create helixApi
                    member _.Whispers with get () = Whispers.create helixApi
            }
