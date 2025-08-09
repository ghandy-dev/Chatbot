namespace Commands


module Api =

    open Http

    type CatFact = {
        Fact: string
        Length: int
    }

    [<Literal>]
    let private ApiUrl = "https://catfact.ninja"

    let private catFactUrl = $"{ApiUrl}/fact"

    let getCatFact () =
        async {
            let request = Request.request catFactUrl
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<CatFact>
                |> Result.mapError _.StatusCode
        }

[<AutoOpen>]
module CatFacts =

    open FsToolkit.ErrorHandling
    open Api

    let catFact () =
        asyncResult {
            let! fact = getCatFact () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Cat Fact")
            return Message fact.Fact
        }
