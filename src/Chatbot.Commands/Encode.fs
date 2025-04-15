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

    let encode args =
        let runEncode f (s: string seq) = f (s |> String.concat " ") |> Message

        match args with
        | [] | [ _ ] -> Message "No encoder and/or text provided"
        | encoder :: input ->
            match encoder with
            | "base64" -> runEncode base64 input
            | "rot13" -> runEncode rot13 input
            | "caesar" ->
                let mutable iShift = 0
                match input with
                | shift :: text when Int32.TryParse(shift, &iShift) -> runEncode (caesar iShift) text
                | _ -> runEncode (caesar (System.Random.Shared.Next(1, 27))) input
            | _ -> Message "Unknown encoder specified"
