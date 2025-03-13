namespace Commands

[<AutoOpen>]
module Time =

    open System

    let [<Literal>] private DateTimeFormat = "yyyy/MM/dd HH:mm:ss"

    let geolocationService = Services.services.GeolocationService

    let time args =
        async {
            match args with
            | [] -> return Message $"{DateTime.UtcNow.ToString(DateTimeFormat)} (UTC)"
            | address ->
                let timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

                match! geolocationService.GetSearchAddress (address |> String.concat " ") with
                | Error err -> return Message err
                | Ok location ->
                    match! geolocationService.GetTimezone location.Position.Lat location.Position.Lon timestamp with
                    | Error err -> return Message err
                    | Ok timezone ->
                        let unixTime = timestamp + int64 timezone.DstOffset + int64 timezone.RawOffset
                        let dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString(DateTimeFormat)

                        return Message $"{dateTime} {timezone.TimeZoneName}"
        }
