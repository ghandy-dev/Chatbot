namespace UrbanDictionary

module Api =

    open Types

    let [<Literal>] private ApiUrl = "https://api.urbandictionary.com/v0"

    let private randomUrl = $"{ApiUrl}/random"
    let private searchUrl term = $"{ApiUrl}/define?term={term}"

    let random () =
        async {
            match! Http.getFromJsonAsync<Terms>randomUrl with
            | Error (content, statusCode) ->
                Logging.error $"Urban Dictionary API error: {content}" (new System.Net.Http.HttpRequestException("Urban Dictionary API error", null, statusCode))
                return Error "Urban Dictionary API error"
            | Ok definitions -> return Ok definitions.list
        }

    let search term =
        async {
            match! Http.getFromJsonAsync<Terms>(searchUrl term) with
            | Error (content, statusCode) ->
                Logging.error $"Urban Dictionary API error: {content}" (new System.Net.Http.HttpRequestException("Urban Dictionary API error", null, statusCode))
                return Error "Urban Dictionary API error"
            | Ok definitions -> return Ok definitions.list
        }