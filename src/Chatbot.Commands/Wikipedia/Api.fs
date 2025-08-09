namespace Wikipedia

module Api =

    open Http
    open Types

    let [<Literal>] private ApiUrl = "https://api.wikimedia.org/core/v1/wikipedia"

    let private search query numberOfResults = $"{ApiUrl}/en/search/page?q={query}&limit={numberOfResults}"

    let getWikiResults query =
        async {
            let url = search query 1
            let request = Request.request url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Pages>
                |> Result.mapError _.StatusCode
        }
