namespace Chatbot.Commands

[<AutoOpen>]
module TopStreams =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch

    let topStreams (twitchService: TwitchService) context =
        asyncResult {
            let! streams = twitchService.Streams.GetStreams 10 |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Streams")

            return
                match streams with
                | [] -> [ Message "No one is streaming!" ]
                | streams ->
                    let topStreams =
                        streams
                        |> Seq.map (fun s -> $"""@{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                        |> String.join ", "

                    [ Message topStreams ]
        }
