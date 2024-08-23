namespace Chatbot.Commands

[<AutoOpen>]
module TextTransform =

    open System

    let private random = Random.Shared

    let private toUpper text =
        text |> String.concat " " |> (fun t -> t.ToUpper())

    let private toLower text =
        text |> String.concat " " |> (fun t -> t.ToLower())

    let private reverse text =
        text |> String.concat " " |> Seq.rev |> Array.ofSeq |> (fun s -> new string (s))

    let private shuffle text =
        let array = text |> Array.ofSeq
        array |> Array.iteri (fun n _ -> Array.swap array n (random.Next(array.Length)) |> ignore)
        array |> String.concat " "

    let private explode text =
        text |> String.concat " " |> Array.ofSeq |> (fun s -> String.Join(" ", s))

    let private transforms =
        [
            ("uppercase", toUpper)
            ("lowercase", toLower)
            ("reverse", reverse)
            ("shuffle", shuffle)
            ("explode", explode)
        ]
        |> Map.ofList

    let private transform' transform words =
        match transforms |> Map.tryFind transform with
        | Some t ->
            let text = t words
            Message text
        | None -> Message $"Unknown transform: \"{transform}\""

    let texttransform args =
        match args with
        | [] -> Message "No transform/text provided"
        | [ _ ] -> Message "No transform and/or text provided"
        | transform :: words -> transform' transform words
