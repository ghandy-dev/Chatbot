namespace Chatbot.Commands

[<AutoOpen>]
module Rot13 =

    open System

    let caeser (text: string) (shift: int) =
        let chars = text.ToCharArray()

        chars
        |> Array.map (fun c ->
            if Char.IsAsciiLetter(c) then
                let shiftBase = if Char.IsUpper(c) then 65 else 97
                ((((c |> int) - shiftBase + shift) % 26) + shiftBase) |> char
            else
                c
        )
        |> (fun cs -> new string (cs))

    // caeser cipher, but if you apply it again to the output then it decodes it
    let rot13 text = caeser text 13

    let base64 (text: string) =
        text |> System.Text.Encoding.UTF8.GetBytes |> System.Convert.ToBase64String

    let encode args =
        match args with
        | "base64" :: input -> base64 (String.concat " " input) |> Message |> Ok
        | "rot13" :: input -> rot13 (String.concat " " input) |> Message |> Ok
        | "caeser" :: input -> caeser (String.concat " " input) (System.Random.Shared.Next(1, 27)) |> Message |> Ok
        | _ -> Error "Unknown encoder specified"
