namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.Geolocation.Azure.Types
open Chatbot.Infrastructure.Services.Geolocation.Google.Types

type IGeolocationService =
    abstract member GetTimeZone: double -> double -> int64 -> Async<Result<Timezone, int>>
    abstract member GetReverseAddress: double -> double -> Async<Result<ReverseSearchAddressResult, int>>
    abstract member GetSearchAddress: string -> Async<Result<SearchAddressResult, int>>

type GoogleOptions = {
    GeocodingApiKey: string
    TimezoneApiKey: string
}

type MicrosoftOptions = {
    MapsApiKey: string
}

module GeolocationService =

    open Chatbot.Common
    open Types

    let create env microsoftOptions goolgeOptions =

        let httpClient = env.HttpClient

        let createMicrosoft ()  =

            let baseApiUrl = "https://atlas.microsoft.com"
            let apiKey = microsoftOptions.MapsApiKey

            let apiUrl = $"{baseApiUrl}/search"
            let apiVersion = "api-version=1.0"

            let reverseAddressUrl latitude longitude =
                UrlBuilder.buildUrl
                    $"{apiUrl}/address/reverse/json"
                    [
                        "api-version", apiVersion
                        "query", $"{latitude},{longitude}"
                        "language", "en-GB"
                        "limit", $"%d{1}"
                        "subscription-key", apiKey
                    ]

            let searchAddressUrl address =
                UrlBuilder.buildUrl
                    $"{apiUrl}/address/json"
                    [
                        "api-version", apiVersion
                        "query", address
                        "language", "en-GB"
                        "limit", $"%d{1}"
                        "subscription-key", apiKey
                    ]

            let httpClient = env.HttpClient

            let getReverseAddress (latitude: double) (longitude: double) =
                async {
                    let url = reverseAddressUrl latitude longitude

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<ReverseSearchAddressResult>
                        |> Result.mapError _.StatusCode
                }

            let getSearchAddress (address: string) =
                async {
                    let url = searchAddressUrl address

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<SearchAddressResult>
                        |> Result.mapError _.StatusCode
                }

            getReverseAddress,
            getSearchAddress

        let createGoogle ()  =

            let baseApiUrl = "https://maps.googleapis.com"

            let geocodeApiUrl = $"{baseApiUrl}/maps/api/geocode/json"
            let apiKey = goolgeOptions.TimezoneApiKey

            let geoCodeAddressUrl address =
                UrlBuilder.buildUrl
                    $"{geocodeApiUrl}"
                    [ "address", address ; "key", apiKey ]


            let timezoneUrl latitude longitude timestamp =
                UrlBuilder.buildUrl
                    $"{baseApiUrl}/maps/api/timezone/json"
                    [ "location", $"{latitude},{longitude}" ; "timestamp", $"{timestamp}" ; "key", apiKey ]

            let getTimezone latitude longitude timestamp =
                async {
                    let url = timezoneUrl latitude longitude timestamp

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    match response |> Response.toJsonResult<Timezone> with
                    | Error err -> return Error err.StatusCode
                    | Ok response ->
                        match Status.tryParseError response.Status with
                        | Some Status.Ok -> return Ok response
                        | Some status -> return Error (status |> Status.toHttpStatusCode |> int)
                        | None -> return Error (System.Net.HttpStatusCode.InternalServerError |> int)
                }

            getTimezone

        let getTimeZone = createGoogle ()
        let getReverseAddress, getSearchAddress = createMicrosoft ()

        {
            new IGeolocationService with
                member _.GetTimeZone latitude longitude timestamp = getTimeZone latitude longitude timestamp
                member _.GetReverseAddress latitude longitude = getReverseAddress latitude longitude
                member _.GetSearchAddress address = getSearchAddress address
        }