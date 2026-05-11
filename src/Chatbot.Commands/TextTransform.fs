namespace Chatbot.Commands

[<AutoOpen>]
module TextTransform =

    open System

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError

    let private random = Random.Shared

    let private toUpper text = text |> String.join " " |> _.ToUpper()

    let private toLower text = text |> String.join " " |> _.ToLower()

    let private reverse text = text |> String.join " " |> Seq.rev |> Array.ofSeq |> fun s -> new string (s)

    let private shuffle text =
        let array = text |> Array.ofSeq
        array |> Array.iteri (fun n _ -> Array.swap array n (random.Next(array.Length)) |> ignore)
        array |> String.join " "

    let private explode text =
        text |> String.join " " |> Array.ofSeq |> fun s -> String.Join(" ", s)

    let private alternating (text: string seq) =
        let mutable alternated = false

        text
        |> Seq.map _.ToCharArray()
        |> Seq.mapi (fun i a ->
            a
            |> Array.map(fun c ->
                if Char.IsLetter(c) then
                    alternated <- not alternated
                    if alternated then Char.ToUpper c else Char.ToLower c
                else
                    c
            )
            |> fun s -> new string (s)) |> String.join " "

    let private transforms =
        [
            "uppercase", toUpper
            "lowercase", toLower
            "reverse", reverse
            "shuffle", shuffle
            "explode", explode
            "alternating", alternating
            "alternate", alternating
        ]
        |> Map.ofList

    let texttransform context =
        match context.MessageArgs with
        | [] ->  invalidArgs "No transform/text provided"
        | [ _ ] -> invalidArgs "No transform and/or text provided"
        | transform :: words ->
            match transforms |> Map.tryFind transform with
            | None -> invalidArgs $"Unknown transform: \"{transform}\""
            | Some f ->
                let message = f words

                Ok [ Message message ]