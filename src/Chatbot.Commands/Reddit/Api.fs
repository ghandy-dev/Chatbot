namespace Reddit

module Api =

    open FsToolkit.ErrorHandling

    open Authorization
    open Http
    open Types

    [<Literal>]
    let private ApiUrl = "https://reddit.com"

    [<Literal>]
    let private OAuthApiUrl = "https://oauth.reddit.com"

    let getPosts (subreddit: string) (sorting: string) =
        asyncResult {
            let url =
                match sorting with
                | "hot" -> $"{OAuthApiUrl}/r/{subreddit}/hot.json"
                | "top" -> $"{OAuthApiUrl}/r/{subreddit}/top.json?t=week"
                | "best" -> $"{OAuthApiUrl}/r/{subreddit}/best.json"
                | _ -> failwith "Unsupported post sorting."

            let! token = tokenStore.GetToken TokenType.Reddit

            let request =
                Request.get url
                |> Request.withHeaders [
                    Header.accept ContentType.ApplicationJson
                    Header.authorization <| AuthenticationScheme.bearer token
                ]

            let! response = request |> send Http.client

            return!
                response
                |> Response.toJsonResult<Thing<Listing<T3>>>
                |> Result.mapError _.StatusCode
        }
