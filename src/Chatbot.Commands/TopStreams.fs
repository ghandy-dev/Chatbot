namespace Chatbot.Commands

[<AutoOpen>]
module TopStreams =

    open Chatbot
    open Chatbot.HelixApi
    open TTVSharp.Helix

    let private getStreams =
        async {
            return!
                helixApi.Streams.GetStreamsAsync(new GetStreamsRequest(First = 10)) |> Async.AwaitTask
                |+-> TTVSharp.tryGetData
        }

    let topStreams () =
        async {
            match! getStreams with
            | None -> return Ok <| Message "No streams. Waduheck"
            | Some streams ->
                return
                    streams
                    |> Seq.map (fun s -> $"""{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                    |> String.concat ", "
                    |> Message
                    |> Ok
        }
