namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.FaceIt.Types

type FaceItService =
    abstract member GetPlayerByUsername: username: string -> Async<Result<Players.Player,int>>
    abstract member GetPlayerById: playerId: string -> Async<Result<Players.Player,int>>
    abstract member GetPlayerStats: playerId: string -> Async<Result<Players.PlayerStats,int>>
    abstract member GetPlayerMatchHistory: playerId: string -> limit: int -> Async<Result<Players.MatchHistory,int>>
    abstract member GetMatch: matchId: string -> Async<Result<Matches.Match,int>>
    abstract member GetMatchStats: matchId: string -> Async<Result<Matches.Stats.Match,int>>

type FaceItOptions = {
    ApiKey: string
}

module FaceItService =

    let create env config =

        let apiUrl = "https://open.faceit.com/data/v4"

        let playerByUsernameUrl nickname = $"{apiUrl}/players?nickname=%s{nickname}"
        let playerByIdUrl playerId = $"{apiUrl}/players/%s{playerId}"
        let playerStatsUrl playerId gameId = $"{apiUrl}/players/%s{playerId}/stats/%s{gameId}"
        let playerHistoryUrl playerId gameId limit = $"{apiUrl}/players/%s{playerId}/history?game=%s{gameId}&limit=%d{limit}"
        let matchUrl matchId = $"{apiUrl}/matches/%s{matchId}"
        let matchStatsUrl matchId = $"{apiUrl}/matches/%s{matchId}/stats"

        let apiKey = config.ApiKey

        let httpClient = env.HttpClient

        let getPlayerByUsername player =
            async {
                let url = playerByUsernameUrl player

                let request =
                    Request.get url
                    |> Request.withHeaders [ Header.authorization <| AuthenticationScheme.bearer apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Players.Player>
                    |> Result.mapError _.StatusCode
            }

        let getPlayerById playerId =
            async {
                let url = playerByIdUrl playerId

                let request =
                    Request.get url
                    |> Request.withHeaders [ Header.authorization <| AuthenticationScheme.bearer apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Players.Player>
                    |> Result.mapError _.StatusCode
            }

        let getPlayerStats playerId =
            async {
                let url = playerStatsUrl playerId "cs2"

                let request =
                    Request.get url
                    |> Request.withHeaders [ Header.authorization <| AuthenticationScheme.bearer apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Players.PlayerStats>
                    |> Result.mapError _.StatusCode
            }

        let getPlayerMatchHistory playerId limit =
            async {
                let url = playerHistoryUrl playerId "cs2" limit

                let request =
                    Request.get url
                    |> Request.withHeaders [ Header.authorization <| AuthenticationScheme.bearer apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Players.MatchHistory>
                    |> Result.mapError _.StatusCode
            }

        let getMatch matchId =
            async {
                let url = matchUrl matchId

                let request =
                    Request.get url
                    |> Request.withHeaders [ Header.authorization <| AuthenticationScheme.bearer apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Matches.Match>
                    |> Result.mapError _.StatusCode
            }

        let getMatchStats matchId =
            async {
                let url = matchStatsUrl matchId

                let request =
                    Request.get url
                    |> Request.withHeaders [ Header.authorization <| AuthenticationScheme.bearer apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Matches.Stats.Match>
                    |> Result.mapError _.StatusCode
            }

        {
            new FaceItService with
                member _.GetMatch(matchId: string) = getMatch matchId
                member _.GetMatchStats(matchId: string) = getMatchStats matchId
                member _.GetPlayerById(playerId: string) = getPlayerById playerId
                member _.GetPlayerByUsername(username: string) = getPlayerByUsername username
                member _.GetPlayerMatchHistory(playerId: string) (limit: int) = getPlayerMatchHistory playerId limit
                member _.GetPlayerStats(playerId: string) = getPlayerStats playerId
        }