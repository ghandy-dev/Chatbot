namespace Wikipedia

module Api =

    open FsToolkit.ErrorHandling

    open Http
    open Types.Core
    open Types.Feed

    let [<Literal>] private ApiUrl = "https://api.wikimedia.org/"

    let private searchUrl query numberOfResults = $"{ApiUrl}/core/v1/wikipedia/en/search/page?q={query}&limit={numberOfResults}"
    let private feedUrl date = $"{ApiUrl}/feed/v1/wikipedia/en/featured/{date}"

    let mutable private feed: Option<string * Feed> = None

    let getWikiResults query =
        async {
            let url = searchUrl query 1
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