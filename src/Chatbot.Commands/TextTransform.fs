namespace Commands

[<AutoOpen>]
module TextTransform =

    open System

    let private random = Random.Shared

    let private toUpper text =
        text |> String.concat " " |> (fun t -> t.ToUpper())

    let private toLower text =
        text |> String.concat " " |> (fun t -> t.ToLower())

    let private reverse text =
        text |> String.concat " " |> Seq.rev |> Array.ofSeq |> fun s -> new string (s)

    let private shuffle text =
        let array = text |> Array.ofSeq
        array |> Array.iteri (fun n _ -> Array.swap array n (random.Next(array.Length)) |> ignore)
        array |> String.concat " "

    let private explode text =
        text |> String.concat " " |> Array.ofSeq |> fun s -> String.Join(" ", s)

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
            |> fun s -> new string (s)) |> String.concat " "

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
