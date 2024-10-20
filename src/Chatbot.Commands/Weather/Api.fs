namespace Commands.Api

module Weather =

    open Commands.Types.Weather

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    let [<Literal>] private  apiUrl = "https://atlas.microsoft.com/weather"

    let private apiKey = Configuration.Microsoft.config.Weather.ApiKey

    let private apiVersion = "api-version=1.1"

    let private currentWeather latitude longitude = $"{apiUrl}/currentConditions/json?{apiVersion}&query={latitude},{longitude}&subscription-key={apiKey}"

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
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error err -> return Error $"Azure Weather API HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let getCurrentWeather latitude longitude =
        async {
            let url = currentWeather latitude longitude
            return! getFromJsonAsync<CurrentConditionsResult> url
        }
