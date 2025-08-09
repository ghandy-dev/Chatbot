namespace FaceIt

module Api =

    open System.Net.Http

    open FsToolkit.ErrorHandling

    open Configuration
    open Http
    open Types

    [<Literal>]
    let private ApiUrl = "https://open.faceit.com/data/v4"

    let private playerByName nickname = $"players?nickname={nickname}"
    let private playerById playerId = $"players/{playerId}"
    let private playerStats playerId gameId = $"players/{playerId}/stats/{gameId}"
    let private playerHistory playerId gameId limit = $"players/{playerId}/history?game={gameId}&limit={limit}"
    let private ``match`` matchId = $"/matches/{matchId}"
    let private matchStats matchId = $"/matches/{matchId}/stats"

    let apiKey = appConfig.FaceIt.ApiKey

    let headers = [
        Header.authorization <| AuthenticationScheme.bearer apiKey
    ]

    let getPlayer player =
        async {
            let url = $"{ApiUrl}/{playerByName player}"

            let request =
                Request.request url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Players.Player>
                |> Result.mapError _.StatusCode
        }

    let getPlayerById playerId =
        async {
            let url = $"{ApiUrl}/{playerById playerId}"

            let request =
                Request.request url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Players.Player>
                |> Result.mapError _.StatusCode
        }

    let getPlayerStats playerId =
        async {
            let url = $"""{ApiUrl}/{playerStats playerId "cs2"}"""

            let request =
                Request.request url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Players.PlayerStats>
                |> Result.mapError _.StatusCode
        }

    let getPlayerMatchHistory playerId limit =
        async {
            let url = $"""{ApiUrl}/{playerHistory playerId "cs2" limit}"""

            let request =
                Request.request url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Players.MatchHistory>
                |> Result.mapError _.StatusCode
        }

    let getMatch matchId =
        async {
            let url = $"{ApiUrl}/{``match`` matchId}"

            let request =
                Request.request url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Matches.Match>
                |> Result.mapError _.StatusCode
        }

    let getMatchStats matchId =
        async {
            let url = $"{ApiUrl}/{matchStats matchId}"

            let request =
                Request.request url
                |> Request.withHeaders headers

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Matches.Stats.Match>
                |> Result.mapError _.StatusCode
        }
