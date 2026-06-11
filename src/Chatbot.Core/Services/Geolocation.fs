namespace Chatbot.Core.Services

module Geolocation =

    open System.Text.Json.Serialization

    module Azure =

        module Types =

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

    module Google =

        module Types =

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

    open FsToolkit.ErrorHandling

    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types
    open Azure.Types
    open Google.Types

    type GoogleOptions = {
        GeocodingApiKey: string
        TimezoneApiKey: string
    }

    type MicrosoftOptions = {
        MapsApiKey: string
    }

    type IGeolocationService =
        abstract member GetTimeZone: double -> double -> int64 -> Async<Result<Timezone, int>>
        abstract member GetReverseAddress: double -> double -> Async<Result<ReverseSearchAddressResult, int>>
        abstract member GetSearchAddress: string -> Async<Result<SearchAddressResult, int>>

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