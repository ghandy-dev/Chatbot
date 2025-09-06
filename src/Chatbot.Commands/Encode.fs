namespace Commands

[<AutoOpen>]
module Encode =

    open System

    let private caesar (shift: int) (text: string) =
        let chars = text.ToCharArray()

        chars
        |> Array.map (fun c ->
            if Char.IsAsciiLetter(c) then
                let shiftBase = if Char.IsUpper(c) then 65 else 97
                ((c |> int) - shiftBase + shift) % 26 + shiftBase |> char
            else
                c
        )
        |> String

    // caesar cipher, but if you apply it again to the output then it decodes it
    let private rot13 text = caesar 13 text

    let private base64 (text: string) =
        text |> System.Text.Encoding.UTF8.GetBytes |> System.Convert.ToBase64String

    let encode context =
        let runEncode f (s: string) = f s |> Message |> Ok

        match context.Args with
        | [] | [ _ ] -> Error <| InvalidArgs "No encoder and/or text provided"
        | encoder :: input ->
            let text = input |> String.concat " "
            match encoder with
            | "base64" -> runEncode base64 text
            | "rot13" -> runEncode rot13 text
            | "caesar" ->
                match input with
                | shift :: rest ->
                    match Parsing.tryParseInt shift with
                    | Some n -> runEncode (caesar n) (rest |> String.concat " ")
                    | None -> runEncode (caesar (System.Random.Shared.Next(1, 27))) text
                | _ -> runEncode (caesar (System.Random.Shared.Next(1, 27))) text
            | _ -> Error <| InvalidArgs "Unknown encoder specified"
