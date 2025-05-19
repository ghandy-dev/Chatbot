namespace Reddit

module Api =

    open Types
    open Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    let [<Literal>] private ApiUrl = "https://reddit.com"
    let [<Literal>] private OAuthApiUrl = "https://oauth.reddit.com"

    let userAgent = configuration.Item("UserAgent")

    let getFromJsonAsync<'a> (url: string) (accessToken: string) =
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
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error err ->
                let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask
                return Error(content, err.statusCode)
        }

    let getPosts (subreddit: string) (sorting: string) (accessToken: string) =
        async {
            let url =
                match sorting with
                | "hot" -> $"{OAuthApiUrl}/r/{subreddit}/hot.json"
                | "top" -> $"{OAuthApiUrl}/r/{subreddit}/top.json?t=week"
                | "best" -> $"{OAuthApiUrl}/r/{subreddit}/best.json"
                | _ -> failwith "Unsupported post sorting."

            match! getFromJsonAsync<Thing<Listing<T3>>> url accessToken with
            | Error (err, statusCode) ->
                Logging.error $"Reddit API error: {statusCode}, {err}" (exn())
                return Error (err, statusCode)
            | Ok data -> return Ok data
        }
