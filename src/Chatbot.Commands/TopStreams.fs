namespace Commands

[<AutoOpen>]
module TopStreams =

    open Twitch

    let topStreams () =
        async {
            match! Helix.Streams.getStreams 10 with
            | None -> return Message "Twitch API error"
            | Some streams ->
                match streams |> List.ofSeq with
                | [] -> return Message "No one is streaming!"
                | streams ->
                    return
                        streams
                        |> List.map (fun s -> $"""{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                        |> String.concat ", "
                        |> Message
        }
