module IVR

open System

open FsToolkit.ErrorHandling

open Http

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
let private searchUrl (channel: string) (user: string) (query: string) (limit: int) (reverseOpt: string option) (offset: int) = $"""{LogsApiUrl}/channel/{channel}/user/{user}/search?q={query}&limit={limit}{match reverseOpt with | Some "true" -> "&reverse=true" | _ -> ""}&offset={offset}"""
let private lastLineUrl (channel: string) (user: string) = $"{LogsApiUrl}/channel/{channel}/user/{user}/?limit=1&reverse=true"
let private linesUrl (channel: string) (from: string) (``to``: string) (limit: int) = $"{LogsApiUrl}/channel/{channel}?from={from}&to={``to``}&limit={limit}&reverse=true"

let getEmoteByName (emote: string) =
    async {
        let url = getEmoteDataUrl emote false
        let request = Request.get url
        let! response =  request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<Emote>
            |> Result.mapError _.StatusCode
    }

let getSubAge (user: string) (channel: string) =
    async {
        let url = subAgeUrl user channel
        let request = Request.get url
        let! response =  request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<SubAge>
            |> Result.mapError _.StatusCode
    }

let getChannelRandomLine (channel: string) =
    async {
        let url = randomChannelLineUrl channel
        let request = Request.get url
        let! response =  request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap
                (fun r -> r.Content.Trim([|'\r' ; '\n'|]))
                _.StatusCode
    }

let getUserRandomLine (channel: string) (user: string) =
    async {
        let url = randomUserLineUrl channel user
        let request = Request.get url
        let! response =  request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap
                (fun r -> r.Content.Trim([|'\r' ; '\n'|]))
                _.StatusCode
    }

let search (channel: string) (user: string) (query: string) (reverse: bool) (offset: int) =
    async {
        let reverse = if reverse then Some "true" else None
        let url = searchUrl channel user query 1 reverse offset
        let request = Request.get url
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap
                (fun r -> r.Content.Trim([|'\r' ; '\n'|]))
                _.StatusCode
    }

let getLastLine (channel: string) (user: string) =
    async {
        let url = lastLineUrl channel user
        let request = Request.get url
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap
                (fun r -> r.Content.Trim([|'\r' ; '\n'|]))
                _.StatusCode
    }

let getLines (channel: string) (from: DateTime) (``to``: DateTime) (limit: int) =
    async {
        let fromString = from.ToUniversalTime().ToString(UtcDateTimeStringFormat)
        let toString = ``to``.ToUniversalTime().ToString(UtcDateTimeStringFormat)

        let url = linesUrl channel fromString toString limit
        let request = Request.get url
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap
                _.Content
                _.StatusCode
    }