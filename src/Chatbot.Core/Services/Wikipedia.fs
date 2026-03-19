module Wikipedia

open FsToolkit.ErrorHandling

open Http
open UrlBuilder

open System.Text.Json.Serialization

module Core =

    type Thumbnail = {
        Mimetype: string
        // Size: int option
        Width: int
        Height: int
        // Duration: float option
        Url: string
    }

    type Page = {
        Id: int
        Key: string
        Title: string
        Excerpt: string
        // [<JsonPropertyName("matched_title")>]
        // MatchedTitle: string option
        Description: string
        // Thumbnail: Thumbnail option
    }

    type Pages =
        { Pages: Page list }

module Feed =

    type Article = {
        Type: string
        Title: string
        PageId: int
        [<JsonPropertyName("content_urls")>]
        ContentUrls: ContentUrls
        Extract: string
    }

    and ContentUrls = {
        Desktop: Urls
    }

    and Urls = {
        Page: string
    }

    type MostRead = {
        [<JsonConverter(typeof<NonCompliantDateTimeOffsetConverter>)>]
        Date: System.DateTimeOffset
        Articles: Article list
    }

    type News = {
        Links: Article list
        Story: string
    }

    type OnThisDay = {
        Text: string
        Pages: Article list
        Year: int
    }

    type DidYouKnow = {
        Html: string
        Text: string
    }

    type Feed = {
        [<JsonPropertyName("tfa")>]
        FeaturedArticle: Article
        MostRead: MostRead
        News: News list
        OnThisDay: OnThisDay list
        [<JsonPropertyName("dyk")>]
        DidYouKnow: DidYouKnow list
    }

open Core
open Feed

let [<Literal>] private ApiUrl = "https://api.wikimedia.org/"

let private searchUrl query numberOfResults =
    buildUrl
        $"{ApiUrl}/core/v1/wikipedia/en/search/page"
        [ "query", query ; "limit", numberOfResults ]

let private feedUrl date = $"{ApiUrl}/feed/v1/wikipedia/en/featured/{date}"

let mutable private feed: Option<string * Feed> = None

let getWikiResults query =
    async {
        let url = searchUrl query "1"
        let request = Request.get url
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<Pages>
            |> Result.mapError _.StatusCode
    }

let private getTodaysFeed () =
    async {
        let date = utcNow().ToString("yyyy/MM/dd")

        match feed with
        | Some (key, feed) when key = date -> return Ok feed
        | Some _
        | None ->
            let url = feedUrl date
            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Feed>
                |> Result.mapError _.StatusCode
                |> Result.tee (fun f ->
                    feed <- Some (date, f)
                )
    }

let getDidYouKnow () = getTodaysFeed() |> AsyncResult.map _.DidYouKnow

let getOnThisDay () = getTodaysFeed() |> AsyncResult.map _.OnThisDay

let getNews () = getTodaysFeed() |> AsyncResult.map _.News