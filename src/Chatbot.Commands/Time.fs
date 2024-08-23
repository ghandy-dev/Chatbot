namespace Chatbot.Commands

[<AutoOpen>]
module Time =

    open Google

    open System

    let [<Literal>] private dateTimeFormat = "yyyy/MM/dd HH:mm:ss"

    let time args =
        async {
            match args with
            | [] -> return Message $"{DateTime.UtcNow.ToString(dateTimeFormat)} (UTC)"
            | input ->
                let timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

                match! getLocationGecode input with
                | Error err -> return Message err
                | Ok [] -> return Message "Location not found"
                | Ok (g :: _) ->
                    match! getTimezone g.Geometry.Location.Lat g.Geometry.Location.Lng timestamp with
                    | Error err -> return Message err
                    | Ok timezone ->
                        let unixTime = timestamp + int64 timezone.DstOffset + int64 timezone.RawOffset
                        let dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString(dateTimeFormat)

                        return Message $"{dateTime} {timezone.TimeZoneName}"
        }
