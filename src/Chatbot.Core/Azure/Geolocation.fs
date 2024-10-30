namespace Azure.Types

/// Common

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
