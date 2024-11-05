namespace Commands

[<AutoOpen>]
module CatFacts =

    type CatFact = {
        Fact: string
        Length: int
    }

    let [<Literal>] private ApiUrl = "https://catfact.ninja"
    let private factUrl = $"{ApiUrl}/fact"

    let private getCatFact () =
        async {
            match! Http.getFromJsonAsync<CatFact> factUrl with
            | Error (msg, statusCode) ->
                Logging.error $"Cat Fact API error: {msg}" (new System.Net.Http.HttpRequestException("", null, statusCode))
                return None
            | Ok catFact -> return Some catFact
        }

    let catFact () =
        async {
            match! getCatFact() with
            | None -> return Message "Error getting cat fact"
            | Some fact -> return Message fact.Fact
        }
