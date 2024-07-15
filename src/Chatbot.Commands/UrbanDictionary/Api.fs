namespace Chatbot.Commands.UrbanDictionary

module Api =

    open Types
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response


    let [<Literal>] private apiUrl = "https://api.urbandictionary.com/v0"

    let private randomUrl = $"{apiUrl}/random"
    let private searchUrl term = $"{apiUrl}/define?term={term}"

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
                let! json = response |> deserializeJsonAsync<'a>
                return Ok json
            | Error e -> return Error $"Http response did not indicate success. {(int)e.statusCode} {e.reasonPhrase}"
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