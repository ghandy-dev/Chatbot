namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.Weather.Types

type IWeatherService =
    abstract member GetCurrentWeather: latitude: double -> longitude: double -> Async<Result<list<CurrentConditions>,int>>

type WeatherConfig = {
    MapsApiKey: string
}

module WeatherService =

    let create env config =

        let baseApiUrl = "https://atlas.microsoft.com/weather"
        let apiKey = config.MapsApiKey

        let currentConditionsUrl latitude longitude =
            UrlBuilder.buildUrl
                $"{baseApiUrl}/currentConditions/json"
                [ "api-version", "1.1" ; "query", $"{latitude},{longitude}" ; "language", "en-GB" ; "subscription-key", apiKey ]

        let httpClient = env.HttpClient

        let getCurrentWeather (latitude: double) (longitude: double) =
            async {
                let url = currentConditionsUrl latitude longitude

                let request = Request.get url
                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<CurrentConditionsResult>
                    |> Result.eitherMap _.Results _.StatusCode
            }

        {
            new IWeatherService with
                member _.GetCurrentWeather latitude longitude = getCurrentWeather latitude longitude
        }