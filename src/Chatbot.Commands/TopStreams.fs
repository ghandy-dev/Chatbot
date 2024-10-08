namespace Chatbot.Commands

[<AutoOpen>]
module TopStreams =

    open TTVSharp

    let topStreams () =
        async {
            match! Helix.Streams.getStreams 10 with
            | Error err -> return Message err
            | Ok streams ->
                match streams |> List.ofSeq with
                | [] -> return Message "No one is streaming!"
                | streams ->
                    return
                        streams
                        |> List.map (fun s -> $"""{s.UserName} - {s.GameName} ({s.ViewerCount.ToString("N0")})""")
                        |> String.concat ", "
                        |> Message
        }
