namespace Azure

open Http

[<AutoOpen>]
module private Shared =

    let [<Literal>] BaseApiUrl = "https://atlas.microsoft.com"
    let apiKey = Configuration.Microsoft.config.Maps.ApiKey

module Maps =

    module Weather =

        open Azure.Types

        let private apiUrl = $"{BaseApiUrl}/weather"
        let private apiVersion = "api-version=1.1"

        let private currentWeather latitude longitude =
            $"{apiUrl}/currentConditions/json?{apiVersion}&query={latitude},{longitude}&language=en-GB&subscription-key={apiKey}"

        let getCurrentWeather (latitude: double) (longitude: double) =
            async {
                let url = currentWeather latitude longitude

                match! getFromJsonAsync<CurrentConditionsResult> url with
                | Error(content, statusCode) ->
                    Logging.error
                        $"Weather API error: {content}"
                        (new System.Net.Http.HttpRequestException("Azure API error", null, statusCode = statusCode))

                    return Error "Weather API Error"
                | Ok result ->
                    match result.Results with
                    | [] -> return Error "Location not found"
                    | weather :: _ -> return Ok weather
            }

    module Geolocation =

        open Azure.Types

        let private apiUrl = $"{BaseApiUrl}/search"
        let private apiVersion = "api-version=1.0"

        let private reverseAddressUrl latitude longitude =
            $"{apiUrl}/address/reverse/json?api-version={apiVersion}&query={latitude},{longitude}&language=en-GB&subscription-key={apiKey}"

        let private searchAddressUrl (address: 'a) =
            $"{apiUrl}/address/json?api-version={apiVersion}&query={address}&language=en-GB&limit=1&subscription-key={apiKey}"

        let getReverseAddress (latitude: double) (longitude: double) =
            async {
                let url = reverseAddressUrl latitude longitude

                match! getFromJsonAsync<ReverseSearchAddressResult> url with
                | Error(content, statusCode) ->
                    Logging.error
                        $"Geolocation API error: {content}"
                        (new System.Net.Http.HttpRequestException("Geolocation API error", null, statusCode = statusCode))

                    return Error "Geolocation API Error"
                | Ok result ->
                    match result.Addresses with
                    | [] -> return Error "Address not found"
                    | address :: _ -> return Ok address
            }

        let getSearchAddress (address: string) =
            async {
                let url = searchAddressUrl address

                match! getFromJsonAsync<SearchAddressResult> url with
                | Error(content, statusCode) ->
                    Logging.error
                        $"Geolocation API error: {content}"
                        (new System.Net.Http.HttpRequestException("Geolocation API error", null, statusCode = statusCode))

                    return Error "Geolocation API Error"
                | Ok result ->
                    match result.Results with
                    | [] -> return Error "Address not found"
                    | address :: _ -> return Ok address
            }
