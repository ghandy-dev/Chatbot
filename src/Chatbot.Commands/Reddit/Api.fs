namespace Chatbot.Commands.Api

module Reddit =

    open Chatbot.Commands.Types.Reddit
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    [<Literal>]
    let private apiUrl = "https://reddit.com"

    [<Literal>]
    let private oAuthApiUrl = "https://oauth.reddit.com"

    [<Literal>]
    let private authUrl = "https://www.reddit.com/api/v1/access_token"

    let mutable accessToken: AccessToken = Unchecked.defaultof<_>

    let userAgent = configuration.Item("UserAgent")

    let getAccessToken () =
        async {
            if (box accessToken <> null) && accessToken.ExpiresAt > DateTime.UtcNow then
                return Ok accessToken.AccessToken
            else
                use! response =
                    http {
                        POST authUrl
                        Accept MimeTypes.applicationJson
                        AuthorizationUserPw Reddit.config.ClientId Reddit.config.ClientSecret
                        UserAgent userAgent
                        body
                        formUrlEncoded [ ("grant_type", "client_credentials") ]
                    }
                    |> sendAsync

                match toResult response with
                | Ok response ->
                    let! oAuthToken = response |> deserializeJsonAsync<OAuthToken>

                    accessToken <- {
                        AccessToken = oAuthToken.AccessToken
                        ExpiresAt = DateTime.UtcNow.AddSeconds(oAuthToken.ExpiresIn)
                    }

                    return Ok accessToken.AccessToken
                | Error err -> return Error $"[{err.statusCode}] {err.reasonPhrase}: Failed to request access token."
        }

    let getFromJsonAsync<'a> url accessToken =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                    AuthorizationBearer accessToken
                    UserAgent userAgent
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! posts = response |> deserializeJsonAsync<'a>
                return Ok posts
            | Error e -> return Error $"Http response did not indicate success. {(int)e.statusCode} {e.reasonPhrase}"
        }

    let getPosts subreddit sorting accessToken =
        async {
            let url =
                match sorting with
                | "hot" -> $"{oAuthApiUrl}/r/{subreddit}/hot.json"
                | "top" -> $"{oAuthApiUrl}/r/{subreddit}/top.json?t=week"
                | _ -> failwith "Unsupported post sorting."

            return! getFromJsonAsync<Thing<Listing<T3>>> url accessToken
        }
