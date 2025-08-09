module Weather

open System
open System.Net.Http

open FsToolkit.ErrorHandling

open Configuration
open Http

type WeatherUnit = {
    Unit: string
    UnitType: int // https://learn.microsoft.com/en-us/rest/api/maps/weather/get-current-conditions?view=rest-maps-2024-04-01&tabs=HTTP#unittype
    Value: float
}

type IconCode =
    | DN_Cloudy = 7
    | DN_Dreary = 8
    | DN_Fog = 11
    | DN_Showers = 12
    | DN_ThunderStorms = 15
    | DN_Rain = 18
    | DN_Flurries = 19
    | DN_Snow = 22
    | DN_Ice = 24
    | DN_Sleet = 25
    | DN_FreezingRain = 26
    | DN_RainAndSnow = 29
    | DN_Hot = 30
    | DN_Cold = 31
    | DN_Windy = 32
    | D_Sunny = 1
    | D_MostlySunny = 2
    | D_PartlySunny = 3
    | D_IntermittentClouds = 4
    | D_HazySunshine = 5
    | D_MostlyCloudy = 6
    | D_MostlyCloudyWithShowers = 13
    | D_PartlySunnyWithShowers = 14
    | D_MostlyCloudyWithThunderStorms = 16
    | D_MostlyCloudyWithFlurries = 20
    | D_PartlySunnyWithFlurries = 21
    | D_MostlyCloudyWithSnow = 23
    | N_Clear = 33
    | N_MostlyClear = 34
    | N_PartlyCloudy = 35
    | N_IntermittentClouds = 36
    | N_HazyMoonlight = 37
    | N_MostlyCloudy = 38
    | N_PartlyCloudyWithShowers = 39
    | N_MostlyCloudyWithShowers = 40
    | N_PartlyCloudyWithThunderstorms = 41
    | N_MostlyCloudyWithThunderstorms = 42
    | N_MostlyCloudyWithFlurries = 43
    | N_MostlyCloudyWithSnow = 44

type CurrentConditionsResult = { Results: CurrentConditions list }

and CurrentConditions = {
    ApparentTemperature: WeatherUnit
    Celiing: WeatherUnit
    CloudCover: int
    DateTime: DateTimeOffset
    DewPoint: WeatherUnit
    HasPrecipitation: bool
    IconCode: IconCode
    IsDayTime: bool
    ObstructionsToVisibility: string
    PastTwentyFourHourTemperatureDeparture: WeatherUnit
    Phrase: string // Summary of current weather condition
    PrecipitationSummary: PrecipitationSummary
    Pressure: WeatherUnit
    PressureTendency: PressureTendency
    RealFeelTemperature: WeatherUnit
    RealFeelTemperatureShade: WeatherUnit
    RelativeHumidity: int
    Temperature: WeatherUnit
    TemperatureSummary: TemperatureSummary
    UvIndex: int
    UvIndexPhrase: string
    Visibility: WeatherUnit
    WetBulbTemperature: WeatherUnit
    Wind: WindDetails
    WindChillTemperature: WeatherUnit
    WindGust: WindDetails
}

and PrecipitationSummary = {
    PastEighteenHours: WeatherUnit
    PastHour: WeatherUnit
    PastNineHours: WeatherUnit
    PastSixHours: WeatherUnit
    PastThreeHours: WeatherUnit
    PastTwelveHours: WeatherUnit
    PastTwentyFourHours: WeatherUnit
}

and PressureTendency = {
    Code: string
    LocalizedDescription: string
}

and TemperatureSummary = {
    PastSixHours: PastHoursTemperature
    PastTwelveHours: PastHoursTemperature
    PastTwentyFourHours: PastHoursTemperature
}

and PastHoursTemperature = {
    Maximum: WeatherUnit
    Minimum: WeatherUnit
}

and WindDetails = {
    Direction: WindDirection option
    Speed: WeatherUnit
}

and WindDirection = {
    Degrees: float
    LocalizedDescription: string
}

[<Literal>]
let private BaseApiUrl = "https://atlas.microsoft.com"

let private apiKey = appConfig.Microsoft.Maps.ApiKey

let private apiUrl = $"{BaseApiUrl}/weather"
let private apiVersion = "api-version=1.1"

let private currentWeather latitude longitude =
    $"{apiUrl}/currentConditions/json?{apiVersion}&query={latitude},{longitude}&language=en-GB&subscription-key={apiKey}"

let getCurrentWeather (latitude: double) (longitude: double) =
    async {
        let url = currentWeather latitude longitude

        let request = Request.request url
        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<CurrentConditionsResult>
            |> Result.eitherMap _.Results _.StatusCode
    }
