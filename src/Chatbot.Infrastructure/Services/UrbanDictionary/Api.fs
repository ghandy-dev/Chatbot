namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.UrbanDictionary.Types

type UrbanDictionaryService =
    abstract member Random: unit -> Async<Result<Term list, int>>
    abstract member Search: term: string -> Async<Result<Term list, int>>

module UrbanDictionaryService =

    let create env =

        let apiUrl = "https://api.urbandictionary.com/v0"
        let randomUrl = $"{apiUrl}/random"
        let searchUrl term = $"{apiUrl}/define?term=%s{term}"

        let httpClient = env.HttpClient

        let random () =
            async {
                let request = Request.get randomUrl
                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Terms>
                    |> Result.eitherMap _.list _.StatusCode
            }

        let search term =
            async {
                let url = searchUrl term

                let request = Request.get url
                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<Terms>
                    |> Result.eitherMap _.list _.StatusCode
            }

        {
            new UrbanDictionaryService with
                member _.Random () = random ()
                member _.Search term = search term
        }