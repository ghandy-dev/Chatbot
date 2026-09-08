namespace Chatbot.Infrastructure.Services.Geolocation.Azure.Types

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

namespace Chatbot.Infrastructure.Services.Geolocation.Google.Types

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

        static member tryParseError =
            function
            | "OK" -> Some Ok
            | "ZERO_RESULTS" -> Some ZeroResults
            | "OVER_DAILY_LIMIT" -> Some OverDailyLimit
            | "OVER_QUERY_LIMIT" -> Some OverQueryLimit
            | "REQUEST_DENIED" -> Some RequestDenied
            | "INVALID_REQUEST" -> Some InvalidRequest
            | "UNKNOWN_ERROR" -> Some UnknownError
            | _ -> None

        static member toHttpStatusCode =
            function
            | Ok -> System.Net.HttpStatusCode.OK
            | ZeroResults -> System.Net.HttpStatusCode.NotFound
            | OverDailyLimit -> System.Net.HttpStatusCode.TooManyRequests
            | OverQueryLimit -> System.Net.HttpStatusCode.TooManyRequests
            | RequestDenied -> System.Net.HttpStatusCode.Forbidden
            | InvalidRequest -> System.Net.HttpStatusCode.BadRequest
            | UnknownError -> System.Net.HttpStatusCode.InternalServerError