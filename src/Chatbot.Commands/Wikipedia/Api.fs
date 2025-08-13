namespace Wikipedia

module Api =

    open System
    open System.Text.RegularExpressions

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Http
    open Types.Core
    open Types.Feed

    let [<Literal>] private ApiUrl = "https://api.wikimedia.org/"

    let private searchUrl query numberOfResults = $"{ApiUrl}/core/v1/wikipedia/en/search/page?q={query}&limit={numberOfResults}"
    let private feedUrl date = $"{ApiUrl}/feed/v1/wikipedia/en/featured/{date}"

    let private cache = System.Collections.Concurrent.ConcurrentDictionary<string, Feed> ()

    let getWikiResults query =
        async {
            let url = searchUrl query 1
            let request = Request.request url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Pages>
                |> Result.mapError _.StatusCode
        }

    let private getTodaysFeed () =
        async {
            let date = utcNow().ToString("yyyy/MM/dd")

            match cache |> Dict.tryGetValue date with
            | None ->
                let url = feedUrl date
                let request = Request.request url
                let! response = request |> Http.send Http.client

                return
                    response
                    |> Response.toJsonResult<Feed>
                    |> Result.mapError _.StatusCode
                    |> Result.tee (fun feed ->
                        cache[date] <- feed
                    )
            | Some feed -> return Ok feed
        }

    let getDidYouKnow () = getTodaysFeed() |> AsyncResult.map _.DidYouKnow

    let getOnThisDay () = getTodaysFeed() |> AsyncResult.map _.OnThisDay

    let getNews () = getTodaysFeed() |> AsyncResult.map _.News