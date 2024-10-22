module Authorization

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

let private getTwitchTokenAsync (authClient: OAuthClient) =
    async {
        let refreshToken = Configuration.Twitch.config.RefreshToken
        let clientId = Configuration.Twitch.config.ClientId
        let clientSecret = Configuration.Twitch.config.ClientSecret

        let! response = authClient.RefreshTokenAsync(clientId, clientSecret, refreshToken) |> Async.AwaitTask

        if response.Error <> null then
            let statusCode = enum<System.Net.HttpStatusCode> response.Error.Status
            return Error (new System.Net.Http.HttpRequestException(response.Error.Message, null, statusCode))
        else
            return Ok response.Token
    }

let private getRedditTokenAsync () =
    async {
        use! response =
            let clientId = Configuration.Reddit.config.ClientId
            let clientSecret = Configuration.Reddit.config.ClientSecret

            http {
                POST "https://www.reddit.com/api/v1/access_token"
                Accept MimeTypes.applicationJson
                AuthorizationUserPw clientId clientSecret
                UserAgent(Configuration.configuration.Item("UserAgent"))
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
            match tokenType with
            | Twitch ->
                match tokenStoreDict.TryGetValue(tokenType) with
                | true, token when not <| maybeHasExpired token.ExpiresAt -> return Some token.AccessToken
                | _ ->
                    match! getTwitchTokenAsync authClient with
                    | Error err ->
                        Logging.error "Failed to get reddit access token" err
                        return None
                    | Ok token ->
                        tokenStoreDict[tokenType] <- { AccessToken = token.AccessToken ; ExpiresAt = Some token.ExpiresAt }
                        return Some token.AccessToken
            | Reddit ->
                match tokenStoreDict.TryGetValue(tokenType) with
                | true, token when not <| maybeHasExpired token.ExpiresAt -> return Some token.AccessToken
                | _ ->
                    match! getRedditTokenAsync () with
                    | Error err ->
                        Logging.error "Failed to get reddit access token" err
                        return None
                    | Ok token ->
                        tokenStoreDict[tokenType] <- { AccessToken = token.AccessToken ; ExpiresAt = token.ExpiresAt }
                        return Some token.AccessToken
        }

let tokenStore = new TokenStore()
