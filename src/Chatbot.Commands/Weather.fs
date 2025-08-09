namespace Commands

[<AutoOpen>]
module Weather =

    open FsToolkit.ErrorHandling

    open CommandError
    open Geolocation.Azure
    open Weather

    let private weatherCodeToEmoji iconCode =
        match iconCode with
        | IconCode.DN_Cloudy -> "☁️"
        | IconCode.DN_Dreary -> "☁️"
        | IconCode.DN_Fog -> "🌫️"
        | IconCode.DN_Showers -> "🌧️"
        | IconCode.DN_ThunderStorms -> "⛈️"
        | IconCode.DN_Rain -> "🌧️"
        | IconCode.DN_Flurries -> "🌨️"
        | IconCode.DN_Snow -> "🌨️"
        | IconCode.DN_Ice -> "❄️"
        | IconCode.DN_Sleet -> "🌨️"
        | IconCode.DN_FreezingRain -> "🌧️"
        | IconCode.DN_RainAndSnow -> "💧❄️"
        | IconCode.DN_Hot -> "🌡️"
        | IconCode.DN_Cold -> "❄️"
        | IconCode.DN_Windy -> "🌬️"
        | IconCode.D_Sunny -> "☀️"
        | IconCode.D_MostlySunny -> "🌤️"
        | IconCode.D_PartlySunny -> "⛅"
        | IconCode.D_IntermittentClouds -> "⛅"
        | IconCode.D_HazySunshine -> "☀️"
        | IconCode.D_MostlyCloudy -> "☁️"
        | IconCode.D_MostlyCloudyWithShowers -> "🌧️"
        | IconCode.D_PartlySunnyWithShowers -> "🌦️"
        | IconCode.D_MostlyCloudyWithThunderStorms -> "⛈️"
        | IconCode.D_MostlyCloudyWithFlurries -> "🌨️"
        | IconCode.D_PartlySunnyWithFlurries -> "🌥️❄️"
        | IconCode.D_MostlyCloudyWithSnow -> "🌨️"
        | IconCode.N_Clear -> "🌃"
        | IconCode.N_MostlyClear -> "🌃"
        | IconCode.N_PartlyCloudy -> "☁️"
        | IconCode.N_IntermittentClouds -> "☁️"
        | IconCode.N_HazyMoonlight -> "🌕"
        | IconCode.N_MostlyCloudy -> "☁️"
        | IconCode.N_PartlyCloudyWithShowers -> "🌧️"
        | IconCode.N_MostlyCloudyWithShowers -> "🌧️"
        | IconCode.N_PartlyCloudyWithThunderstorms -> "⛈️"
        | IconCode.N_MostlyCloudyWithThunderstorms -> "⛈️"
        | IconCode.N_MostlyCloudyWithFlurries -> "🌨️"
        | IconCode.N_MostlyCloudyWithSnow -> "🌨️"
        | _ -> ""

    let private geolocationService = Services.services.GeolocationService
    let private weatherService = Services.services.WeatherService

    let private processWeatherResult (geocoding: SearchAddressResultItem) (weather: CurrentConditions) =
        let location = geocoding.Address.FreeformAddress
        let emoji = weatherCodeToEmoji weather.IconCode
        let summary = weather.Phrase
        let temperature = $"{weather.Temperature.Value}°{weather.Temperature.Unit}"
        let perceivedTemperature = $"{weather.ApparentTemperature.Value}°{weather.ApparentTemperature.Unit}"

        let wind =
            match weather.Wind.Direction with
            | Some (dir: WindDirection) -> $"Wind: {weather.Wind.Speed.Value} {weather.Wind.Speed.Unit} {dir.LocalizedDescription}"
            | None -> $"Wind: {weather.Wind.Speed.Value} {weather.Wind.Speed.Unit}"

        let precipitation = $"Precipitation {weather.PrecipitationSummary.PastHour.Value} {weather.PrecipitationSummary.PastHour.Unit}"
        let uv = $"UV: {weather.UvIndexPhrase}"

        Message $"{location} {emoji} {summary} {temperature} - feels like {perceivedTemperature}, {wind}, {precipitation}, {uv}"


    let weather args =
        asyncResult {
            match args with
            | [] -> return! invalidArgs "No location provided"
            | address ->
                let! geocoding = geolocationService.GetSearchAddress (address |> String.concat " ") |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Geolocation")
                let! weather = weatherService.GetCurrentWeather geocoding.Position.Lat geocoding.Position.Lon |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Weather")

                match weather with
                | [] -> return Message "No weather conditions reported for location"
                | w :: _ -> return processWeatherResult geocoding w
            }
