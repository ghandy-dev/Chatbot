namespace Commands.UrbanDictionary

module Api =

    open Types
    open Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response


    let [<Literal>] private ApiUrl = "https://api.urbandictionary.com/v0"

    let private randomUrl = $"{ApiUrl}/random"
    let private searchUrl term = $"{ApiUrl}/define?term={term}"

    let userAgent = configuration.Item("UserAgent")

    let getFromJsonAsync<'a> url =
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
            | Error err -> return Error $"UrbanDictionary API HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let random () =
        async {
            match! getFromJsonAsync<Terms>randomUrl with
            | Error err -> return Error err
            | Ok definitions -> return Ok definitions.list
        }

    let search term =
        async {
            match! getFromJsonAsync<Terms>(searchUrl term) with
            | Error err -> return Error err
            | Ok definitions -> return Ok definitions.list
        }