namespace Chatbot.Commands.Api

module Weather =

    open Chatbot.Commands.Types.Weather

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    [<Literal>]
    let private apiUrl = "https://atlas.microsoft.com/weather"

    let apiKey = Chatbot.Configuration.Microsoft.config.Weather.ApiKey

    let apiVersion = "api-version=1.1"

    let currentWeather latitude longitude = $"{apiUrl}/currentConditions/json?{apiVersion}&query={latitude},{longitude}&subscription-key={apiKey}"

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
                return!
                    response
                    |> deserializeJsonAsync<'a>
                    |-> Ok
            | Error e -> return Error $"Http response did not indicate success. {(int) e.statusCode} {e.reasonPhrase}"
        }

    let getCurrentWeather latitude longitude =
        async {
            let url = currentWeather latitude longitude
            return! getFromJsonAsync<CurrentConditionsResult> url
        }
