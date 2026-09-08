namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Infrastructure.Services.Wikipedia.Types

type WikipediaService =
    abstract member GetWikiResults: query: string -> Async<Result<Pages, int>>
    abstract member GetDidYouKnow: unit -> Async<Result<DidYouKnow list, int>>
    abstract member GetOnThisDay: unit -> Async<Result<OnThisDay list, int>>
    abstract member GetNews: unit -> Async<Result<News list, int>>

module WikipediaService =

    open Chatbot.Common
    open Chatbot.Common.Caching
    open Chatbot.Common.Http

    let create env =

        let apiUrl = "https://api.wikimedia.org"

        let searchUrl query numberOfResults =
            UrlBuilder.buildUrl
                $"{apiUrl}/core/v1/wikipedia/en/search/page"
                [ "q", query ; "limit", numberOfResults ]

        let feedUrl date = $"{apiUrl}/feed/v1/wikipedia/en/featured/%s{date}"

        let cache = env.Cache
        let httpClient = env.HttpClient

        let cacheKey = "wikipedia_feed"

        let getTodaysFeed () =
            async {
                let date = today ()

                match cache |> MemoryCache.tryGetValue<System.DateOnly * Feed> cacheKey with
                | Some (cachedDate: System.DateOnly, feed) when cachedDate = date -> return Ok feed
                | _ ->
                    let url = feedUrl (date.ToString("yyyy/MM/dd"))
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
                let url = searchUrl query "1"
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