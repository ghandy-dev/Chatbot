namespace Commands

[<AutoOpen>]
module Time =

    open System

    open FsToolkit.ErrorHandling

    let [<Literal>] private DateTimeFormat = "yyyy/MM/dd HH:mm:ss"

    let geolocationService = Services.services.GeolocationService

    let time context =
        asyncResult {
            match context.Args with
            | [] -> return Message $"{DateTime.UtcNow.ToString(DateTimeFormat)} (UTC)"
            | address ->
                let timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                let! location = geolocationService.GetSearchAddress (address |> String.concat " ") |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Geolocation")
                let! timezone = geolocationService.GetTimezone location.Position.Lat location.Position.Lon timestamp |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Geolocation")
                let unixTime = timestamp + int64 timezone.DstOffset + int64 timezone.RawOffset
                let dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString(DateTimeFormat)

                return Message $"{dateTime} {timezone.TimeZoneName}"
        }
