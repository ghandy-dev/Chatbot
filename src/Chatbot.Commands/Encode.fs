namespace Commands

[<AutoOpen>]
module Rot13 =

    open System

    let caesar (text: string) (shift: int) =
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
    let rot13 text = caesar text 13

    let base64 (text: string) =
        text |> System.Text.Encoding.UTF8.GetBytes |> System.Convert.ToBase64String

    let encoders =
        [
            "base64", base64
            "rot13", rot13
            "caesar", (fun s -> caesar s (System.Random.Shared.Next(1, 27)))
        ]
        |> Map.ofList

    let encode args =
        match args with
        | [] -> Message "No encoder/text provided"
        | [ _ ] -> Message "No encoder or text provided"
        | encoder :: input ->
            match encoders |> Map.tryFind encoder with
            | None -> Message "Unknown encoder specified"
            | Some e -> Message <| e (String.concat "" input)
