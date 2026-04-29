namespace Chatbot.Core.Services


module Wikipedia =


    open FsToolkit.ErrorHandling

    module Types =

        open System.Text.Json.Serialization

        module Common =

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

    module WikipediaUrls =

        open Chatbot.Common

        let [<Literal>] ApiUrl = "https://api.wikimedia.org/"

        let searchUrl query numberOfResults =
            UrlBuilder.buildUrl
                $"{ApiUrl}/core/v1/wikipedia/en/search/page"
                [ "query", query ; "limit", numberOfResults ]

        let feedUrl date = $"{ApiUrl}/feed/v1/wikipedia/en/featured/%s{date}"

    open Types.Common
    open Types.Feed

    type WikipediaService =
        abstract member GetWikiResults: query: string -> Async<Result<Pages, int>>
        abstract member GetDidYouKnow: unit -> Async<Result<DidYouKnow list, int>>
        abstract member GetOnThisDay: unit -> Async<Result<OnThisDay list, int>>
        abstract member GetNews: unit -> Async<Result<News list, int>>

    module WikipediaService =

        open Chatbot.Core
        open Chatbot.Core.Caching
        open Chatbot.Core.Http
        open Chatbot.Core.Types
        open Chatbot.Common

        let create env =
            let cache = env.Cache
            let httpClient = env.HttpClient

            let cacheKey = "wikipedia_feed"

            let getTodaysFeed () =
                async {
                    let date = today ()

                    match cache |> MemoryCache.tryGetValue<System.DateOnly * Feed> cacheKey with
                    | Some (cachedDate: System.DateOnly, feed) when cachedDate = date -> return Ok feed
                    | _ ->
                        let url = WikipediaUrls.feedUrl (date.ToString("yyyy/MM/dd"))
                        let request = Request.get url
                        let! response = request |> Http.send env.HttpClient

                        return
                            response
                            |> Response.toJsonResult<Feed>
                            |> Result.mapError _.StatusCode
                            |> Result.tee (fun f ->
                                env.Cache.CreateEntry((date, f)) |> ignore
                            )
                }

            let getWikiResults query =
                async {
                    let url = WikipediaUrls.searchUrl query "1"
                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Pages>
                        |> Result.mapError _.StatusCode
                }

            let getDidYouKnow () = getTodaysFeed () |> AsyncResult.map _.DidYouKnow

            let getOnThisDay () = getTodaysFeed () |> AsyncResult.map _.OnThisDay

            let getNews () = getTodaysFeed () |> AsyncResult.map _.News

            {
                new WikipediaService with
                    member _.GetWikiResults query = getWikiResults query
                    member _.GetDidYouKnow () = getDidYouKnow ()
                    member _.GetOnThisDay () = getOnThisDay ()
                    member _.GetNews () = getNews ()
            }