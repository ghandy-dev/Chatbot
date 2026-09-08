namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Caching
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.Reddit.Types

type RedditService =
    abstract member GetAccessToken: unit -> Async<Result<AccessToken, int>>
    abstract member GetPosts: subreddit: string -> sorting: string -> Async<Result<Thing<Listing<T3>>, int>>

type RedditOptions = {
    ClientId: string
    ClientSecret: string
}

module RedditService =

    let create env config =

        let apiUrl = "https://reddit.com"
        let oauthApiUrl = "https://oauth.reddit.com"
        let accessTokenUrl = "https://www.reddit.com/api/v1/access_token"

        let subredditHot subreddit = $"{oauthApiUrl}/r/{subreddit}/hot.json"
        let subredditTop subreddit = $"{oauthApiUrl}/r/{subreddit}/top.json?t=week"
        let subredditBest subreddit = $"{oauthApiUrl}/r/{subreddit}/best.json"

        let httpClient = env.HttpClient
        let cache = env.Cache

        let clientId = config.ClientId
        let clientSecret = config.ClientSecret

        let getAccessToken () =
            async {
                match cache |> MemoryCache.tryGetValue<AccessToken> "reddit_accessToken" with
                | Some token when not <| token.hasExpired () -> return Ok token
                | _ ->
                    let request =
                        Request.post accessTokenUrl
                        |> Request.withHeaders [ Header.accept ContentType.ApplicationJson ; Header.authorization <| AuthenticationScheme.basic (clientId, clientSecret) ]
                        |> Request.withBody (Content.FormUrlEncoded [ "grant_type", "client_credentials" ])
                        |> Request.withContentType ContentType.ApplicationFormUrlEncoded

                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<OAuthToken>
                        |> Result.mapError _.StatusCode
                        |> Result.map (fun token -> { AccessToken = token.AccessToken ; ExpiresAt = System.DateTimeOffset.FromUnixTimeSeconds(epochTimeSeconds() + token.ExpiresIn) })
                        |> Result.tee (fun token -> cache |> MemoryCache.set "reddit_accessToken" token |> ignore)
            }

        let getPosts (subreddit: string) (sorting: string) =
            async {
                let url =
                    match sorting with
                    | "hot" -> subredditHot subreddit
                    | "top" -> subredditTop subreddit
                    | "best" -> subredditBest subreddit
                    | _ -> failwith "Unsupported post sorting."

                match! getAccessToken () with
                | Ok token ->
                    let request =
                        Request.get url
                        |> Request.withHeaders [
                            Header.accept ContentType.ApplicationJson
                            Header.authorization <| AuthenticationScheme.bearer token.AccessToken
                        ]

                    let! response = request |> send httpClient

                    return
                        response
                        |> Response.toJsonResult<Thing<Listing<T3>>>
                        |> Result.mapError _.StatusCode
                | Error err ->
                    return Error err

            }

        {
            new RedditService with
                member _.GetAccessToken () = getAccessToken ()
                member _.GetPosts subreddit sorting = getPosts subreddit sorting
        }
