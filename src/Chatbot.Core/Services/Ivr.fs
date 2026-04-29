namespace Chatbot.Core.Services

module Ivr =

    open System

    module Types =

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

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types
    open Types

    type IvrService =
        abstract member GetChannelRandomLine: string -> Async<Result<string, int>>
        abstract member GetUserRandomLine: string -> string -> Async<Result<string, int>>
        abstract member GetLastLine: string -> string -> Async<Result<string, int>>
        abstract member GetLines: string -> DateTime -> DateTime -> int -> Async<Result<string, int>>
        abstract member Search: string -> string -> string -> bool -> int -> Async<Result<string, int>>
        abstract member GetEmoteByName: string -> Async<Result<Emote, int>>
        abstract member GetSubAge: string -> string -> Async<Result<SubAge, int>>

    module IvrService =

        let [<Literal>] private ApiUrl = "https://api.ivr.fi/v2"
        let [<Literal>] private LogsApiUrl = "https://logs.ivr.fi"

        let create env =

            let getEmoteDataUrl (emote: string) (id: bool) =
                UrlBuilder.buildUrl
                    $"{ApiUrl}/twitch/emotes/{emote}"
                    [ "id", if id then "true" else "false" ]

            let subAgeUrl (user: string) (channel: string) = $"{ApiUrl}/twitch/subage/{user}/{channel}"
            let randomChannelLineUrl (channel: string) = $"{LogsApiUrl}/channel/{channel}/random"
            let randomUserLineUrl (channel: string) (user: string) = $"{LogsApiUrl}/channel/{channel}/user/{user}/random"

            let searchUrl (channel: string) (user: string) (query: string) (limit: int) (reverseOpt: string option) (offset: int) =
                UrlBuilder.buildUrl
                    $"{LogsApiUrl}/channel/{channel}/user/{user}/search"
                    [ "q", query ; "limit", $"%d{limit}" ; match reverseOpt with | Some "true" -> "reverse", "true" | _ -> "reverse", "false" ; "offset", $"%d{offset}" ]

            let lastLineUrl (channel: string) (user: string) = $"{LogsApiUrl}/channel/{channel}/user/{user}/?limit=1&reverse=true"

            let linesUrl (channel: string) (from: string) (``to``: string) (limit: int) =
                UrlBuilder.buildUrl
                    $"{LogsApiUrl}/channel/{channel}"
                    [ "from", from ; "to", ``to`` ; "limit", $"%d{limit}" ; "reverse", "true" ]


            let httpClient = env.HttpClient

            let getEmoteByName (emote: string) =
                async {
                    let url = getEmoteDataUrl emote false
                    let request = Request.get url
                    let! response =  request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Emote>
                        |> Result.mapError _.StatusCode
                }

            let getSubAge (user: string) (channel: string) =
                async {
                    let url = subAgeUrl user channel
                    let request = Request.get url
                    let! response =  request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<SubAge>
                        |> Result.mapError _.StatusCode
                }

            let getChannelRandomLine (channel: string) =
                async {
                    let url = randomChannelLineUrl channel
                    let request = Request.get url
                    let! response =  request |> Http.send httpClient

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
                    let! response =  request |> Http.send httpClient

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
                    let! response = request |> Http.send httpClient

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
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toResult
                        |> Result.eitherMap
                            _.Content
                            _.StatusCode
                }

            let search (channel: string) (user: string) (query: string) (reverse: bool) (offset: int) =
                async {
                    let reverse = if reverse then Some "true" else None
                    let url = searchUrl channel user query 1 reverse offset
                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toResult
                        |> Result.eitherMap
                            (fun r -> r.Content.Trim([|'\r' ; '\n'|]))
                            _.StatusCode
                }

            {
                new IvrService with
                    member _.GetChannelRandomLine channel = getChannelRandomLine channel
                    member _.GetLastLine channel user = getLastLine channel user
                    member _.GetLines channel from ``to`` limit = getLines channel from ``to`` limit
                    member _.GetUserRandomLine channel user = getUserRandomLine channel user
                    member _.Search channel user query reverse offset = search channel user query reverse offset
                    member _.GetEmoteByName emote = getEmoteByName emote
                    member _.GetSubAge user channel = getSubAge user channel
            }