namespace Chatbot.Commands

[<AutoOpen>]
module TopStreams =

    open TTVSharp

    let topStreams () =
        async {
            match! Helix.Streams.getStreams 10 with
            | None -> return Error "Error fetching top streams"
            | Some streams ->
                match streams |> List.ofSeq with
                | [] -> return Ok <| Message "No one is streaming!"
                | streams ->
                    return
                        streams
                        |> List.map (fun s -> $"""{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                        |> String.concat ", "
                        |> Message
                        |> Ok
        }
