namespace Chatbot.Commands

[<AutoOpen>]
module Time =

    open Google

    open System

    let [<Literal>] private dateTimeFormat = "yyyy/MM/dd HH:mm:ss"

    let time args =
        async {
            match args with
            | [] -> return Ok <| Message $"{DateTime.UtcNow.ToString(dateTimeFormat)} (UTC)"
            | input ->
                let timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

                match!
                    getLocationGecode input
                    |> Result.bindAsync (fun g -> getTimezone g.Geometry.Location.Lat g.Geometry.Location.Lng timestamp)
                with
                | Error err -> return Error err
                | Ok timezone ->
                    let unixTime = timestamp + ((int64)timezone.DstOffset) + ((int64)timezone.RawOffset)
                    let dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString(dateTimeFormat)

                    return Ok <| Message $"{dateTime} {(timezone.TimeZoneName)}"
        }
