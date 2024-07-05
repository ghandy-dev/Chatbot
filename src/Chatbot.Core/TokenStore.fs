namespace Chatbot

module Authorization =

    open Chatbot

    open System
    open System.Collections.Concurrent

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open TTVSharp.Auth

    type AccessToken = {
        AccessToken: string
        ExpiresAt: DateTime option
    }

    type TokenType =
        | Twitch
        | Reddit

    let hasExpired dateTime = DateTime.UtcNow > dateTime

    let maybeHasExpired dateTime =
        match dateTime with
        | None -> false
        | Some dateTime -> hasExpired dateTime

    let getTwitchTokenAsync (authClient: OAuthClient) =
        async {
            let refreshToken = Configuration.Twitch.config.RefreshToken
            let clientId = Configuration.Twitch.config.ClientId
            let clientSecret = Configuration.Twitch.config.ClientSecret

            let! response = authClient.RefreshTokenAsync(clientId, clientSecret, refreshToken) |> Async.AwaitTask

            if response.Error <> null then
                return
                    Error
                        $"Error occurred trying to refresh Twitch token. Status Code: {response.Error.Status}, Message: {response.Error.Message}"
            else
                return Ok response.Token
        }

    let getRedditTokenAsync () =
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
            | Error err -> return Error $"Error occurred trying to refresh Reddit token: Status Code: {err.statusCode |> int}"
            | Ok response ->
                let! token = response |> deserializeJsonAsync<AccessToken>
                return Ok token
        }


    type TokenStore() =

        let logger = Logging.createLogger<TokenStore>
        let authClient = new OAuthClient()
        let tokenStoreDict = ConcurrentDictionary<TokenType, AccessToken>()

        member _.GetToken (tokenType) =
            async {
                match tokenType with
                | Twitch ->
                    match tokenStoreDict.TryGetValue(tokenType) with
                    | true, token when maybeHasExpired token.ExpiresAt -> return Some token.AccessToken
                    | _ ->
                        match! getTwitchTokenAsync authClient with
                        | Error err ->
                            logger.LogError(err)
                            return None
                        | Ok token ->
                            tokenStoreDict[tokenType] <- { AccessToken = token.AccessToken ; ExpiresAt = Some token.ExpiresAt }
                            return Some token.AccessToken
                | Reddit ->
                    match tokenStoreDict.TryGetValue(tokenType) with
                    | true, token when maybeHasExpired token.ExpiresAt -> return Some token.AccessToken
                    | _ ->
                        match! getRedditTokenAsync () with
                        | Error err ->
                            logger.LogError(err)
                            return None
                        | Ok token ->
                            tokenStoreDict[tokenType] <- { AccessToken = token.AccessToken ; ExpiresAt = token.ExpiresAt }
                            return Some token.AccessToken
            }

    let tokenStore = new TokenStore()
