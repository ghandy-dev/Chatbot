namespace Chatbot.Commands

[<AutoOpen>]
module CatFacts =

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    type CatFact = {
        Fact: string
        Length: int
    }

    [<Literal>]
    let private apiUrl = "https://catfact.ninja"

    let catFact () =
        async {
            use! response =
                http {
                    GET(apiUrl + "/fact")
                    CacheControl "no-cache"
                    Accept MimeTypes.applicationJson
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! fact = response |> deserializeJsonAsync<CatFact>
                return Ok <| Message fact.Fact
            | Error response ->
                // TODO: log errors like this, return something else?
                let! rawResponse = toTextAsync response
                return Error $"{response.statusCode} {rawResponse}"
        }
