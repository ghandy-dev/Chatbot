module Geolocation

open Configuration
open Http

open System.Text.Json.Serialization

module Azure =

    type SearchSummary = {
        FuzzyLevel: int
        GeoBias: LatLongPairAbbreviated option
        Limit: int
        NumResults: int
        Offset: int
        Query: string
        QueryTime: int
        QueryType: string
        TotalResults: int
    }

    and LatLongPairAbbreviated = {
        Lat: double
        Lon: double
    }

    type Address = {
        BoundingBox: BoundingBoxCompassNotation
        BuildingNumber: string
        Country: string
        CountryCode: string
        CountryCodeISO3: string
        CountrySecondarySubdivision: string
        CountrySubdivision: string
        CountrySubdivisionCode: string
        CountrySubdivisionName: string
        CountryTertiarySubdivision: string
        CrossStreet: string
        ExtendedPostalCode: string
        FreeformAddress: string
        LocalName: string
        Municipality: string
        MunicipalitySubdivision: string
        Neighbourhood: string
        PostalCode: string
        RouteNumbers: string
        Street: string
        StreetName: string
        StreetNameAndNumber: string
        StreetNumber: string
    }

    and BoundingBoxCompassNotation = {
        Entity: Entity
        NorthEast: string
        SouthWest: string
    }

    and Entity = { Position: string }

    // Search Address Result

    type SearchAddressResult = {
        Results: SearchAddressResultItem list
        Summary: SearchSummary
    }

    and SearchAddressResultItem = {
        Address: Address
        AddressRanges: AddressRanges option
        DataSources: DataSources
        DetourTime: int option
        Dist: float option
        EntityType: string option
        EntryPoints: EntryPoint[] option
        Id: string
        Info: string option
        POI: PointOfInterest option
        Position: LatLongPairAbbreviated
        Score: double
        Type: string
        Viewport: BoundingBox
    }

    and AddressRanges = {
        From: LatLongPairAbbreviated
        RangeLeft: string
        RangeRight: string
        To: LatLongPairAbbreviated
    }

    and DataSources = { Geometry: Geometry }

    and Geometry = { Id: string }

    and PointOfInterest = {
        Brands: Brand list
        Categories: string list
        Categoryset: PointOfInterestCategorySet list
        Classifications: Classification list
        Name: string
        OpeningHours: OperatingHours
        Phone: string
        Url: string
    }

    and Brand = { Name: string }

    and PointOfInterestCategorySet = { Id: string }

    and Classification = {
        Code: string
        Names: ClassificationName list
    }

    and ClassificationName = {
        Name: string
        NameLocale: string
    }

    and OperatingHours = {
        Mode: string
        TimeRanges: OperatingHoursTimeRange list
    }

    and OperatingHoursTimeRange = {
        EndTime: OperatingHoursTime
        StartTime: OperatingHoursTime
    }

    and OperatingHoursTime = {
        Date: string
        Hour: int
        Minute: string
    }

    and EntryPoint = {
        Position: LatLongPairAbbreviated
        Type: EntryPointType
    }

    and EntryPointType = {
        Main: string
        Minor: string
    }

    and BoundingBox = {
        btmRightPoint: LatLongPairAbbreviated
        topLeftPoint: LatLongPairAbbreviated
    }


    // Reverse Search Address Result

    type ReverseSearchAddressResult = {
        Addresses: ReverseSearchAddressResultItem list
        Summary: SearchSummary
    }

    and ReverseSearchAddressResultItem = {
        Address: Address
        MatchType: string
        Position: string // latitude,longitude
        RoadUse: RoadUseType
    }

    and RoadUseType = {
        Arterial: string
        LimitedAccess: string
        LocalStreet: string
        Ramp: string
        Rotary: string
        Terminal: string
    }


    let [<Literal>] BaseApiUrl = "https://atlas.microsoft.com"
    let apiKey = appConfig.Microsoft.Maps.ApiKey

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


module Google =

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

    let [<Literal>] BaseApiUrl = "https://maps.googleapis.com"

    let private geocodeApiUrl = $"{BaseApiUrl}/maps/api/geocode/json?"
    let private geocodeApiKey = appConfig.Google.Geocoding.ApiKey
    let private geoCodeAddressUrl address = $"{geocodeApiUrl}address={address}&key={geocodeApiKey}"


    let private timezoneApiKey = appConfig.Google.Timezone.ApiKey
    let private TimezoneApiUrl = $"{BaseApiUrl}/maps/api/timezone/json?"
    let private timezoneUrl latitude longitude timestamp = $"{TimezoneApiUrl}location={latitude},{longitude}&timestamp={timestamp}&key={timezoneApiKey}"


    let getTimezone latitude longitude timestamp =
        async {
            let url = timezoneUrl latitude longitude timestamp

            match! Http.getFromJsonAsync<Timezone> url with
            | Error (content, statusCode) ->
                Logging.error $"Timezone API error: {content}" (new System.Net.Http.HttpRequestException("Timezone API error", null, statusCode = statusCode))
                return Error "Timezone API Error"
            | Ok response ->
                match Status.tryParse response.Status with
                | Some Status.Ok -> return Ok response
                | _ -> return Error $"Timezone API error: {response.Status}"
        }
