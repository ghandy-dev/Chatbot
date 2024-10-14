namespace Commands.Api

module Wikipedia =

    open Commands.Types.Wikipedia
    open Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    let [<Literal>] private ApiUrl = "https://api.wikimedia.org/core/v1/wikipedia"

    let search query numberOfResults = $"{ApiUrl}/en/search/page?q={query}&limit={numberOfResults}"

    let userAgent = configuration.Item("UserAgent")

    let private getFromJsonAsync<'a> url =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                    UserAgent userAgent
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error err -> return Error $"Wikipedia API HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let getWikiPage query =
        async {
            let url = search query 1

            return! getFromJsonAsync<Pages> url
        }
