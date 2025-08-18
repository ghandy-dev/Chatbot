module Authorization

open System
open System.Collections.Concurrent
open System.Net.Http

open FSharpPlus
open FsToolkit.ErrorHandling
open TTVSharp.Auth

open Configuration
open Http

type AccessToken = {
    AccessToken: string
    ExpiresAt: DateTimeOffset
} with

    member this.hasExpired () =
        DateTimeOffset.UtcNow > this.ExpiresAt

type TokenType =
    | Twitch
    | Reddit

type TokenStore() =

    let authClient = new OAuthClient()
    let tokenCache = ConcurrentDictionary<TokenType, AccessToken>()

    let getTwitchToken () =
        async {
            let refreshToken = appConfig.Twitch.RefreshToken
            let clientId = appConfig.Twitch.ClientId
            let clientSecret = appConfig.Twitch.ClientSecret

            let! response = authClient.RefreshTokenAsync(clientId, clientSecret, refreshToken) |> Async.AwaitTask

            if response.Error <> null then
                return Error response.Error.Status
            else
                return Ok { AccessToken = response.Data.AccessToken ; ExpiresAt = response.Data.ExpiresAt }
        }

    let getRedditToken () =
        async {
            let url = "https://www.reddit.com/api/v1/access_token"
            let clientId = appConfig.Reddit.ClientId
            let clientSecret = appConfig.Reddit.ClientSecret

            let request =
                Request.request url
                |> Request.withMethod Method.Post
                |> Request.withHeaders [ Header.accept ContentType.applicationJson ; Header.authorization <| AuthenticationScheme.basic (clientId, clientSecret) ]
                |> Request.withBody (Content.FormUrlEncoded [ "grand_type", "client_credentials" ])
                |> Request.withContentType ContentType.applicationFormUrlEncoded

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult
                |> Result.mapError _.StatusCode
                |> Result.map (fun token -> { AccessToken = token.AccessToken ; ExpiresAt = token.ExpiresAt })
        }

    member _.GetToken (tokenType) =
        async {
            let logError err = Logging.error $"Http error: {err}. Failed to get {tokenType} access token" exn
            let updateCache token = tokenCache[tokenType] <- token
            let handleTokenResult = AsyncResult.teeError logError >> AsyncResult.tee updateCache >> AsyncResult.map _.AccessToken

            let maybeToken = tokenCache |> Dict.tryGetValue tokenType

            match tokenType, maybeToken with
            | _, Some token when not <| token.hasExpired () ->
                return Ok token.AccessToken
            | Twitch, _ ->
                return!
                    getTwitchToken ()
                    |> handleTokenResult
            | Reddit, _ ->
                return!
                    getRedditToken ()
                    |> handleTokenResult
        }

let tokenStore = new TokenStore()
