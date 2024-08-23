namespace Chatbot.Commands

[<AutoOpen>]
module Weather =

    open Chatbot.Commands.Api.Weather
    open Types.Weather
    open Google

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

    let private processWeatherResult geocoding weather =
        let time = weather.DateTime.ToString("dd MMM HH:mm")
        let location = geocoding.FormattedAddress
        let emoji = weatherCodeToEmoji weather.IconCode
        let summary = weather.Phrase
        let temperature = $"{weather.Temperature.Value}°{weather.Temperature.Unit}"

        let perceivedTemperature =
            $"{weather.ApparentTemperature.Value}°{weather.ApparentTemperature.Unit}"

        let wind =
            match weather.Wind.Direction with
            | Some dir -> $"Wind: {weather.Wind.Speed.Value} {weather.Wind.Speed.Unit} {dir.LocalizedDescription}"
            | None -> $"Wind: {weather.Wind.Speed.Value} {weather.Wind.Speed.Unit}"

        let dayOrNight = if weather.IsDayTime then "🌅" else "🌛"
        let precipitation = $"Precipitation {weather.PrecipitationSummary.PastHour.Value} {weather.PrecipitationSummary.PastHour.Unit}"
        let uv = $"UV: {weather.UvIndexPhrase}"

        Message $"{dayOrNight} {location} 🕒 Updated at: [{time}] {emoji} {summary} {temperature} - feels like {perceivedTemperature}, {wind}, {precipitation}, {uv}"

    let weather args =
        async {
            match args with
            | [] -> return Message "No location provided"
            | location ->
                match! getLocationGecode location with
                | Error statusCode -> return Message statusCode
                | Ok [] -> return Message "Location not found"
                | Ok (geocoding :: _) ->
                    let latitude = geocoding.Geometry.Location.Lat
                    let longitude = geocoding.Geometry.Location.Lng

                    match! getCurrentWeather latitude longitude with
                    | Error statusCode -> return Message statusCode
                    | Ok w ->
                        match w.Results with
                        | [] -> return Message "No weather results found for {geocoding.FormattedAddress}"
                        | weather :: _ -> return processWeatherResult geocoding weather
            }
