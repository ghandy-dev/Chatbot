namespace Chatbot.Commands.Api

module Wikipedia =

    open Chatbot.Commands.Types.Wikipedia
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    [<Literal>]
    let private apiUrl = "https://api.wikimedia.org/core/v1/wikipedia"

    let search query numberOfResults =
        $"{apiUrl}/en/search/page?q={query}&limit={numberOfResults}"

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
                let! posts = response |> deserializeJsonAsync<'a>
                return Ok posts
            | Error e -> return Error $"Http response did not indicate success. {(int) e.statusCode} {e.reasonPhrase}"
        }

    let getWikiPage query =
        async {
            let url = search query 1

            return! getFromJsonAsync<Pages> url
        }
