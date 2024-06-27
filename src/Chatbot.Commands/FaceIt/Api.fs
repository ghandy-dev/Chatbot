namespace Chatbot.Commands.Api

module FaceIt =

    open Chatbot.Commands.Types.FaceIt
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open Matches
    open Players

    [<Literal>]
    let private apiUrl = "https://open.faceit.com/data/v4"

    let private playerByName nickname = $"players?nickname={nickname}"
    let private playerById playerId = $"players/{playerId}"
    let private playerStats playerId gameId = $"players/{playerId}/stats/{gameId}"

    let private playerHistory playerId gameId limit =
        $"players/{playerId}/history?game={gameId}&limit={limit}"

    let private matches matchId = $"/matches/{matchId}"

    let private getFromJsonAsync<'a> url =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                    AuthorizationBearer FaceIt.config.ApiKey
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error e -> return Error $"Http response did not indicate success. {(int)e.statusCode} {e.reasonPhrase}"
        }

    let getPlayer player =
        async {
            let url = $"{apiUrl}/{playerByName player}"
            return! getFromJsonAsync<Player> url
        }

    let getPlayerById playerId =
        async {
            let url = $"{apiUrl}/{playerById playerId}"
            return! getFromJsonAsync<Player> url
        }

    let getPlayerStats playerId =
        async {
            let url = $"""{apiUrl}/{playerStats playerId "cs2"}"""
            return! getFromJsonAsync<PlayerStats> url
        }

    let getPlayerMatchHistory playerId limit =
        async {
            let url = $"""{apiUrl}/{playerHistory playerId "cs2" limit}"""
            return! getFromJsonAsync<PlayerMatchHistory> url
        }

    let getMatch matchId =
        async {
            let url = $"{apiUrl}/{matches matchId}"
            return! getFromJsonAsync<Match> url
        }
