module Reddit

open System
open System.Text.Json.Serialization

type T3 = {
    Title: string
    Subreddit: string
    Quarantine: bool
    Score: int
    [<JsonPropertyName("created_utc")>]
    CreatedUtc: float
    [<JsonPropertyName("over_18")>]
    Over18: bool
    Url: string
    [<JsonPropertyName("is_self")>]
    IsSelf: bool
    [<JsonPropertyName("crosspost_parent_list")>]
    CrosspostParentList: T3 list option
    [<JsonPropertyName("link_flair_text")>]
    Flair: string option
}

type Listing<'T> = {
    Before: string option
    After: string option
    ModHash: string
    Children: Thing<'T> list
}

and Thing<'T> = {
    Id: string option
    Name: string option
    Kind: string
    Data: 'T
}

type OAuthToken = {
    [<JsonPropertyName("access_token")>]
    AccessToken: string
    [<JsonPropertyName("token_type")>]
    TokenType: string
    [<JsonPropertyName("expires_in")>]
    ExpiresIn: int
    Scope: string
}

open FsToolkit.ErrorHandling

open Authorization
open Http

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
