module Authorization

open Configuration

open System
open System.Collections.Concurrent

open FsHttp
open FsHttp.Request
open FsHttp.Response

open TTVSharp.Auth

type AccessToken = {
    AccessToken: string
    ExpiresAt: DateTimeOffset option
}

type TokenType =
    | Twitch
    | Reddit

let private hasExpired dateTime = DateTimeOffset.UtcNow > dateTime

let private maybeHasExpired (dateTime: DateTimeOffset option) =
    match dateTime with
    | None -> false
    | Some dateTime -> hasExpired dateTime

let private getTwitchToken (authClient: OAuthClient) =
    async {
        let refreshToken = appConfig.Twitch.RefreshToken
        let clientId = appConfig.Twitch.ClientId
        let clientSecret = appConfig.Twitch.ClientSecret

        let! response = authClient.RefreshTokenAsync(clientId, clientSecret, refreshToken) |> Async.AwaitTask

        if response.Error <> null then
            let statusCode = enum<System.Net.HttpStatusCode> response.Error.Status
            return Error (new System.Net.Http.HttpRequestException(response.Error.Message, null, statusCode))
        else
            return Ok response.Data
    }

let private getRedditToken () =
    async {
        use! response =
            let clientId = appConfig.Reddit.ClientId
            let clientSecret = appConfig.Reddit.ClientSecret

            http {
                POST "https://www.reddit.com/api/v1/access_token"
                Accept MimeTypes.applicationJson
                AuthorizationUserPw clientId clientSecret
                UserAgent appConfig.UserAgent
                body
                formUrlEncoded [ ("grant_type", "client_credentials") ]
            }
            |> sendAsync

        match toResult response with
        | Error err ->
            return Error (new System.Net.Http.HttpRequestException("", null, response.statusCode))
        | Ok response ->
            let! token = response |> deserializeJsonAsync<AccessToken>
            return Ok token
    }

type TokenStore() =

    let authClient = new OAuthClient()
    let tokenStoreDict = ConcurrentDictionary<TokenType, AccessToken>()

    member _.GetToken (tokenType) =
        async {
            let maybeToken = tokenStoreDict |> Dictionary.tryGetValue(tokenType)

            match tokenType, maybeToken with
            | Twitch, Some token when not <| maybeHasExpired token.ExpiresAt -> return Some token.AccessToken
            | Twitch, _ ->
                match! getTwitchToken authClient with
                | Error err ->
                    Logging.error "Failed to get reddit access token" err
                    return None
                | Ok token ->
                    tokenStoreDict[tokenType] <- { AccessToken = token.AccessToken ; ExpiresAt = Some token.ExpiresAt }
                    return Some token.AccessToken
            | Reddit, Some token when not <| maybeHasExpired token.ExpiresAt -> return Some token.AccessToken
            | Reddit, _ ->
                match! getRedditToken () with
                | Error err ->
                    Logging.error "Failed to get reddit access token" err
                    return None
                | Ok token ->
                    tokenStoreDict[tokenType] <- { AccessToken = token.AccessToken ; ExpiresAt = token.ExpiresAt }
                    return Some token.AccessToken
        }

let tokenStore = new TokenStore()
