namespace Chatbot.Commands

[<AutoOpen>]
module Time =

    open System

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Geolocation

    let [<Literal>] private DateTimeFormat = "yyyy/MM/dd HH:mm:ss"

    let time (geolocationService: IGeolocationService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return [ Message $"{DateTime.UtcNow.ToString(DateTimeFormat)} (UTC)" ]
            | address ->
                let timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

                match! geolocationService.GetSearchAddress (address |> String.join " ") |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Geolocation") with
                | { Results = [] } -> return [ Message "Location not found" ]
                | { Results = location :: _ } ->
                    let! timezone = geolocationService.GetTimeZone location.Position.Lat location.Position.Lon timestamp |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Geolocation")
                    let unixTime = timestamp + int64 timezone.DstOffset + int64 timezone.RawOffset
                    let dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString(DateTimeFormat)

                    return [ Message $"{dateTime} {timezone.TimeZoneName}" ]
        }
