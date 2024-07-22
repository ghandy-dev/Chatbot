module Google

open FsHttp
open FsHttp.Request
open FsHttp.Response

open System.Text.Json.Serialization

type ApiResponse<'T> = {
    Results: 'T list
    Status: string
}

and Geocoding = {
    [<JsonPropertyName("address_components")>]
    AddressComponents: AddressComponent list
    [<JsonPropertyName("formatted_address")>]
    FormattedAddress: string
    Geometry: Geometry
    [<JsonPropertyName("place_id")>]
    PlaceId: string
    [<JsonPropertyName("plus_code")>]
    PlusCode: PlusCode
    Types: string list
}

and AddressComponent = {
    [<JsonPropertyName("long_name")>]
    LongName: string
    [<JsonPropertyName("short_name")>]
    ShortName: string
    Types: string list
}

and Geometry = {
    Location: Coordinates
    [<JsonPropertyName("location_type")>]
    LocationType: string
    Viewport: ViewPort
}

and Coordinates = {
    Lat: float
    Lng: float
}

and ViewPort = {
    [<JsonPropertyName("north_east")>]
    NorthEast: Coordinates
    [<JsonPropertyName("south_west")>]
    SouthWest: Coordinates
}

and PlusCode = {
    [<JsonPropertyName("compound_code")>]
    CompoundCode: string
    [<JsonPropertyName("global_code")>]
    GlobalCode: string
}

[<RequireQualifiedAccess>]
type Status =
    | Ok
    | ZeroResults
    | OverDailyLimit
    | OverQueryLimit
    | RequestDenied
    | InvalidRequest
    | UnknownError

    static member tryParse =
        function
        | "OK" -> Some Ok
        | "ZERO_RESULTS" -> Some ZeroResults
        | "OVER_DAILY_LIMIT" -> Some OverDailyLimit
        | "OVER_QUERY_LIMIT" -> Some OverQueryLimit
        | "REQUEST_DENIED" -> Some RequestDenied
        | "INVALID_REQUEST" -> Some InvalidRequest
        | "UNKNOWN_ERROR" -> Some UnknownError
        | _ -> None

let apiKey = Chatbot.Configuration.Google.config.Geocoding.ApiKey

let [<Literal>] ApiUrl = "https://maps.googleapis.com/maps/api"

let GeocodeUrl = $"{ApiUrl}/geocode/json?"

let GeocodeAddress address = $"{GeocodeUrl}address={address}&key={apiKey}"

let getFromJson<'T> url =
    async {
        use! response =
            http {
                GET url
                Accept MimeTypes.applicationJson
            }
            |> sendAsync

        match response |> toResult with
        | Error err ->
            return Error $"HTTP status code did not indicate success: Google Geocode API {err.statusCode}"
        | Ok res ->
            return!
                res
                |> deserializeJsonAsync<'T>
                |-> Ok
    }

let getGeocodedAddress (address) =
    async {
        let address = System.Web.HttpUtility.UrlEncode (address |> String.concat "+")
        let url = GeocodeAddress address

        match! getFromJson<ApiResponse<Geocoding>> url with
        | Error err -> return Error err
        | Ok response ->
            match response.Status |> Status.tryParse with
            | Some Status.Ok ->
                match response.Results with
                | [] -> return Error "No results"
                | location :: _ -> return Ok location
            | Some _ -> return Error response.Status
            | _ -> return Error "Unknown status error from Google Geocode API"
    }