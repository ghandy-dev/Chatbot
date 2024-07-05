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

    let userAgent = configuration.Item("UserAgent")

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
