namespace Google.Types

open System.Text.Json.Serialization

type ApiResponse<'T> = {
    Results: 'T list
    Status: string
}

type Geocoding = {
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

type Timezone = {
    DstOffset: int
    RawOffset: int
    Status: string
    TimeZoneId: string
    TimeZoneName: string
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
