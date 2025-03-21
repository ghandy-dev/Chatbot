module IVR

open System

type Emote = {
    ChannelName: string option
    ChannelLogin: string option
    ChannelId: string option
    Artist: Artist option
    EmoteId: string
    EmoteCode: string
    EmoteUrl: string
    EmoteSetId: string option
    EmoteAssetType: string option
    EmoteState: string
    EmoteType: string
    EmoteTier: string option
}

and Artist = {
    DisplayName: string
    Login: string
    Id: string
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

let getEmoteByName (emote: string) =
    async {
        let url = getEmoteDataUrl emote false

        match! Http.getFromJsonAsync<Emote> url with
        | Error (content, statusCode) ->
            Logging.error
                $"IVR API error: {content}"
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
                $"IVR API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }

let getChannelRandomLine (channel: string) =
    async {
        let url = randomChannelLineUrl channel

        match! Http.getAsync url with
        | Error (content, statusCode) ->
            Logging.error
                $"IVR API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }

let getUserRandomLine (channel: string) (user: string) =
    async {
        let url = randomUserLineUrl channel user

        match! Http.getAsync url with
        | Error (content, statusCode) ->
            Logging.error
                $"IVR API error: {content}"
                (new System.Net.Http.HttpRequestException("IVR API error", null, statusCode = statusCode))

            return Error "IVR API error"
        | Ok emote -> return Ok emote
    }
