namespace RiotGames

module Api =

    open Types
    open Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    let private accountUrl (gameName: string) (tagLine: string) = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/%s{gameName}/%s{tagLine}"
    let private summonerUrl (region: string) (puuid: string) = $"https://%s{region}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/%s{puuid}"
    let private leagueEntryUrl (region: string) (summonerId: string) = $"https://%s{region}.api.riotgames.com/lol/league/v4/entries/by-summoner/%s{summonerId}"
    let private matchIdsUrl (puuid: string) = $"https://europe.api.riotgames.com/lol/match/v5/matches/by-puuid/%s{puuid}/ids"
    let private matchUrl (matchId: string) = $"https://europe.api.riotgames.com/lol/match/v5/matches/%s{matchId}"

    let getFromJsonAsync<'T> (url: string) =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                    header "X-Riot-Token" appConfig.RiotGames.ApiKey
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! deserialized = response |> deserializeJsonAsync<'T>
                return Ok deserialized
            | Error err ->
                let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask
                Logging.error $"Riot Games API error: {content}" (new System.Net.Http.HttpRequestException("Riot Games API error", null, statusCode = err.statusCode))
                return Error ($"Riot Games API error {err.statusCode |> int} {err.statusCode}", err.statusCode)
        }

    let getAccount (gameName: string) (tagLine: string) =
        async {
            let url = accountUrl gameName tagLine
            return! getFromJsonAsync<Account> url
        }

    let getSummoner (region: string) (puuid: string) =
        async {
            let url = summonerUrl region puuid
            return! getFromJsonAsync<Summoner> url
        }

    let getLeagueEntries (region: string) (summonerId: string) =
        async {
            let url = leagueEntryUrl region summonerId
            return! getFromJsonAsync<LeagueEntry list> url
        }

    let getMatchList (puuid: string) =
        async {
            let url = matchIdsUrl puuid
            return! getFromJsonAsync<string list> url
        }

    let getMatch (matchId: string) =
        async {
            let url = matchUrl matchId
            return! getFromJsonAsync<Match> url
        }