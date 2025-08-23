namespace RiotGames

module Api =

    open System.Net.Http

    open FsToolkit.ErrorHandling

    open Configuration
    open Http
    open Types

    let private accountUrl (gameName: string) (tagLine: string) = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/%s{gameName}/%s{tagLine}"
    let private summonerUrl (region: string) (puuid: string) = $"https://%s{region}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/%s{puuid}"
    let private leagueEntryUrl (region: string) (summonerId: string) = $"https://%s{region}.api.riotgames.com/lol/league/v4/entries/by-summoner/%s{summonerId}"
    let private matchIdsUrl (puuid: string) = $"https://europe.api.riotgames.com/lol/match/v5/matches/by-puuid/%s{puuid}/ids"
    let private matchUrl (matchId: string) = $"https://europe.api.riotgames.com/lol/match/v5/matches/%s{matchId}"

    let apiKey = appConfig.RiotGames.ApiKey

    let headers = [
        "X-Riot-Token", apiKey
    ]

    let getAccount (gameName: string) (tagLine: string) =
        async {
            let url = accountUrl gameName tagLine

            let request =
                Request.get url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

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
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Summoner>
                |> Result.mapError _.StatusCode
        }

    let getLeagueEntries (region: string) (summonerId: string) =
        async {
            let url = leagueEntryUrl region summonerId

            let request =
                Request.get url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

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
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

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
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Match>
                |> Result.mapError _.StatusCode
        }