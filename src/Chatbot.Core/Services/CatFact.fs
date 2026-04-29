namespace Chatbot.Core.Services

module CatFact =

    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types

    type CatFact = {
        Fact: string
        Length: int
    }

    type ICatFactService =
        abstract member GetCatFact: unit -> Async<Result<CatFact, int>>

    module CatFactService =

        let create env =

            let apiUrl = "https://catfact.ninja"
            let catFactUrl = $"{apiUrl}/fact"

            let httpClient = env.HttpClient

            let getCatFact () =
                async {
                    let request = Request.get catFactUrl
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<CatFact>
                        |> Result.mapError _.StatusCode
                }

            {
                new ICatFactService with
                    member _.GetCatFact () = getCatFact ()
            }