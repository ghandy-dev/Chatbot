namespace Google

[<AutoOpen>]
module private Shared =

    let [<Literal>] ApiUrl = "https://maps.googleapis.com"

module Geolocation =

    open Google.Types

    let private geocodeApiUrl = $"{ApiUrl}/maps/api/geocode/json?"
    let private geocodeApiKey = Configuration.Google.config.Geocoding.ApiKey
    let private geoCodeAddressUrl address = $"{geocodeApiUrl}address={address}&key={geocodeApiKey}"

    let getLocationGeocode (address) =
        async {
            let address = System.Web.HttpUtility.UrlEncode (address |> String.concat "+")
            let url: string = geoCodeAddressUrl address

            match! Http.getFromJsonAsync<ApiResponse<Geocoding>> url with
            | Error (content, statusCode) ->
                Logging.error $"Geolocation API error: {content}" (new System.Net.Http.HttpRequestException("Geolocation API error", null, statusCode = statusCode))
                return Error "Geolocation API Error"
            | Ok response -> return Ok response.Results
        }

module Timezone =

    open Google.Types

    let private timezoneApiKey = Configuration.Google.config.Timezone.ApiKey
    let private TimezoneApiUrl = $"{ApiUrl}/maps/api/timezone/json?"
    let private timezoneUrl latitude longitude timestamp = $"{TimezoneApiUrl}location={latitude},{longitude}&timestamp={timestamp}&key={timezoneApiKey}"

    let getTimezone latitude longitude timestamp =
        async {
            let url = timezoneUrl latitude longitude timestamp

            match! Http.getFromJsonAsync<Timezone> url with
            | Error (content, statusCode) ->
                Logging.error $"Timezone API error: {content}" (new System.Net.Http.HttpRequestException("Timezone API error", null, statusCode = statusCode))
                return Error "Timezone API Error"
            | Ok response -> return Ok response
        }


