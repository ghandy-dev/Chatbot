namespace Commands

[<AutoOpen>]
module CatFacts =

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    type CatFact = {
        Fact: string
        Length: int
    }

    let [<Literal>] private ApiUrl = "https://catfact.ninja"

    let private FactUrl = $"{ApiUrl}/fact"

    let private getFromJsonAsync<'a> url =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error err -> return Error $"CatFacts API HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let private getCatFact () =
        async {
            return! getFromJsonAsync<CatFact> FactUrl
        }

    let catFact () =
        async {
            match! getCatFact() with
            | Ok fact -> return Message fact.Fact
            | Error err -> return Message err
        }
