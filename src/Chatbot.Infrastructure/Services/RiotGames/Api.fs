namespace Chatbot.Infrastructure.Services

open System

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.RiotGames.Types

type RiotGamesService =
    abstract member GetAccount: username: string -> tag: string -> Async<Result<Account, int>>
    abstract member GetSummoner: region: string -> puuid: string -> Async<Result<Summoner, int>>
    abstract member GetLeagueEntries: region: string -> puuid: string -> Async<Result<LeagueEntry list, int>>
    abstract member GetMatchList: puuid: string -> Async<Result<string list, int>>
    abstract member GetMatch: matchId: string -> Async<Result<Match, int>>

type RiotGamesOptions = {
    ApiKey: string
}

// TODO: fix broken url?
module RiotGamesService =

    let create env config =

        let accountUrl (username: string) (tag: string) = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/%s{username |> Uri.EscapeDataString}/%s{tag |> Uri.EscapeDataString}"
        let summonerUrl region puuid = $"https://%s{region}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/%s{puuid}"
        let leagueEntryUrl region puuid = $"https://%s{region}.api.riotgames.com/lol/league/v4/entries/by-puuid/%s{puuid}"
        let matchIdsUrl puuid = $"https://europe.api.riotgames.com/lol/match/v5/matches/by-puuid/%s{puuid}/ids"
        let matchUrl matchId = $"https://europe.api.riotgames.com/lol/match/v5/matches/%s{matchId}"

        let apiKey = config.ApiKey
        let httpClient = env.HttpClient

        let getAccount (username: string) (tag: string) =
            async {
                let url = accountUrl username tag

                let request =
                    Request.get url
                    |> Request.withHeaders [ "X-Riot-Token", apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Account>
                    |> Result.mapError _.StatusCode
            }

        let getSummoner (region: string) (puuid: string) =
            async {
                let url = summonerUrl region puuid

                let request =
                    Request.get url
                    |> Request.withHeaders [ "X-Riot-Token", apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Summoner>
                    |> Result.mapError _.StatusCode
            }

        let getLeagueEntries (region: string) (puuid: string) =
            async {
                let url = leagueEntryUrl region puuid

                let request =
                    Request.get url
                    |> Request.withHeaders [ "X-Riot-Token", apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<LeagueEntry list>
                    |> Result.mapError _.StatusCode
            }

        let getMatchList (puuid: string) =
            async {
                let url = matchIdsUrl puuid

                let request =
                    Request.get url
                    |> Request.withHeaders [ "X-Riot-Token", apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<string list>
                    |> Result.mapError _.StatusCode
            }

        let getMatch (matchId: string) =
            async {
                let url = matchUrl matchId

                let request =
                    Request.get url
                    |> Request.withHeaders [ "X-Riot-Token", apiKey ]

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Match>
                    |> Result.mapError _.StatusCode
            }

        {
            new RiotGamesService with
                member _.GetAccount username tag = getAccount username tag
                member _.GetSummoner region puuid = getSummoner region puuid
                member _.GetLeagueEntries region puuid = getLeagueEntries region puuid
                member _.GetMatchList puuid = getMatchList puuid
                member _.GetMatch puuid = getMatch puuid
        }