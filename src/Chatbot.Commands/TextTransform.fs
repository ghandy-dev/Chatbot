namespace Commands

[<AutoOpen>]
module TextTransform =

    open System

    let private random = Random.Shared

    let private toUpper text =
        text |> String.concat " " |> _.ToUpper()

    let private toLower text =
        text |> String.concat " " |> _.ToLower()

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

    let texttransform args =
        match args with
        | [] -> Error <| InvalidArgs "No transform/text provided"
        | [ _ ] -> Error <| InvalidArgs "No transform and/or text provided"
        | transform :: words ->
            match transforms |> Map.tryFind transform with
            | None -> Error <| InvalidArgs $"Unknown transform: \"{transform}\""
            | Some f ->
                let text = f words
                Ok <| Message text