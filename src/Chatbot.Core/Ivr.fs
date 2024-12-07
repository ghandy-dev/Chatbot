module IVR

open System

open FsHttp
open FsHttp.Request
open FsHttp.Response

type Emote = {
    ChannelName: string option
    ChannelLogin: string option
    ChannelId: string option
    Artist: string option
    EmoteId: string
    EmoteCode: string
    EmoteUrl: string
    EmoteSetId: string option
    EmoteAssetType: string option
    EmoteState: string
    EmoteType: string
    EmoteTier: string option
}

type SubAge = {
    User: User
    Channel: Channel
    StatusHidden: bool option
    FollowedAt: DateTimeOffset option
    Streak: Stats option
    Cumulative: Stats option
    Meta: SubMeta option
}

and User = {
    Id: string
    Login: string
    DisplayName: string
}

and Channel = {
    Id: string
    Login: string
    DisplayName: string
}

and Stats = {
    ElapsedDays: int
    DaysRemaining: int
    Months: int
    End: DateTimeOffset
    Start: DateTimeOffset
}

and SubMeta = {
    Type: string
    Tier: string
    EndsAt: DateTimeOffset option
    RenewsAt: DateTimeOffset option
    GiftMeta: GiftMeta option
}

and GiftMeta = {
    GiftDate: DateTimeOffset
    Gifter: Gifter
}

and Gifter = {
    Id: string
    Login: string
    DisplayName: string
}

let [<Literal>] private ApiUrl = "https://api.ivr.fi/v2"
let [<Literal>] private LogsApiUrl = "https://logs.ivr.fi"

let private getEmoteDataUrl (emote: string) (id: bool) = $"{ApiUrl}/twitch/emotes/{emote}?id=%s{if id then bool.TrueString.ToLower() else bool.FalseString.ToLower()}"
let private subAgeUrl (user: string) (channel: string) = $"{ApiUrl}/twitch/subage/{user}/{channel}"
let private randomChannelLineUrl (channel: string) = $"{LogsApiUrl}/channel/{channel}/random"
let private randomUserLineUrl (channel: string) (user: string) = $"{LogsApiUrl}/channel/{channel}/user/{user}/random"

let private sendRequest (url: string) =
    async {
        use! response =
            http {
                GET url
                Accept MimeTypes.textPlain
            }
            |> sendAsync

        let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask

        match toResult response with
        | Ok _ -> return Ok content
        | Error err -> return Error(content, err.statusCode)
    }

let getEmoteByName (emote: string) =
    async {
        let url = getEmoteDataUrl emote false

        match! Http.getFromJsonAsync<Emote> url with
        | Error (content, statusCode) ->
            Logging.error
                $"Weather API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }

let getSubAge (user: string) (channel: string) =
    async {
        let url = subAgeUrl user channel

        match! Http.getFromJsonAsync<SubAge> url with
        | Error (content, statusCode) ->
            Logging.error
                $"Weather API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }

let getChannelRandomLine (channel: string) =
    async {
        let url = randomChannelLineUrl channel

        match! sendRequest url with
        | Error (content, statusCode) ->
            Logging.error
                $"Weather API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }

let getUserRandomLine (channel: string) (user: string) =
    async {
        let url = randomUserLineUrl channel user

        match! sendRequest url with
        | Error (content, statusCode) ->
            Logging.error
                $"Weather API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }
