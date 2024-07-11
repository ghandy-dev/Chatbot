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
                |+> TTVSharp.tryGetDataResult
        }

    let topStreams () =
        async {
            match! getStreams with
            | Error err -> return Error err
            | Ok response ->
                match response |> List.ofSeq with
                | [] -> return Ok <| Message "No one is streaming!"
                | streams ->
                    return
                        streams
                        |> Seq.map (fun s -> $"""{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                        |> String.concat ", "
                        |> Message
                        |> Ok
        }
