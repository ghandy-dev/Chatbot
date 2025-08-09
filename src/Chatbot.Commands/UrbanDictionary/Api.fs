namespace UrbanDictionary

module Api =

    open FsToolkit.ErrorHandling

    open Http
    open Types

    let [<Literal>] private ApiUrl = "https://api.urbandictionary.com/v0"

    let private randomUrl = $"{ApiUrl}/random"
    let private searchUrl term = $"{ApiUrl}/define?term={term}"

    let random () =
        async {
            let request = Request.request randomUrl
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Terms>
                |> Result.eitherMap _.list _.StatusCode
        }

    let search term =
        async {
            let url = searchUrl term

            let request = Request.request url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Terms>
                |> Result.eitherMap _.list _.StatusCode
        }