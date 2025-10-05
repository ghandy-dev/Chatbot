namespace Commands

[<AutoOpen>]
module TopStreams =

    open FsToolkit.ErrorHandling

    let twitchService = Services.services.TwitchService

    let topStreams _ =
        asyncResult {
            let! streams = twitchService.GetStreams 10 |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Streams")

            return
                match streams with
                | [] -> Message "No one is streaming!"
                | streams ->
                    streams
                    |> Seq.map (fun s -> $"""@{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                    |> String.concat ", "
                    |> Message
        }
