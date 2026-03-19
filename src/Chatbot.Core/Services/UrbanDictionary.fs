module UrbanDictionary

open System
open System.Text.Json.Serialization

type Terms = { list: Term list }

and Term = {
    Definition: string
    Permalink: string
    [<JsonPropertyName("thumbs_up")>]
    ThumbsUp: int
    Author: string
    Word: string
    DefId: int
    [<JsonPropertyName("current_vote")>]
    CurrentVote: string
    [<JsonPropertyName("written_on")>]
    WrittenOn: DateTime
    Example: string
    [<JsonPropertyName("thumbs_down")>]
    ThumbsDown: int
}


open FsToolkit.ErrorHandling

open Http

let [<Literal>] private ApiUrl = "https://api.urbandictionary.com/v0"

let private randomUrl = $"{ApiUrl}/random"
let private searchUrl term = $"{ApiUrl}/define?term={term}"

let random () =
    async {
        let request = Request.get randomUrl
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<Terms>
            |> Result.eitherMap _.list _.StatusCode
    }

let search term =
    async {
        let url = searchUrl term

        let request = Request.get url
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<Terms>
            |> Result.eitherMap _.list _.StatusCode
    }