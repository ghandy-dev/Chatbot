namespace Commands

[<AutoOpen>]
module Fill =

    let [<Literal>] private MaxLength = 500

    let private keys = [ "repeat" ]

    let private repeatFill (accLength: int, nextIndex: int, words: string list) =
        if accLength + words[nextIndex].Length > MaxLength then
            None
        else
            Some (words[nextIndex], (accLength + words[nextIndex].Length + 1, (nextIndex+1) % words.Length, words))

    let private randomFill (accLength: int, words: string list) =
        let word = words |> List.randomChoice
        if accLength + word.Length > MaxLength then
            None
        else
            Some (word, (accLength + word.Length + 1, words))

    let fill args =
        let options = KeyValueParser.parse args keys
        let repeat = options |> Map.tryFind "repeat" |> Option.bind (fun v -> Boolean.tryParse v) |?? true
        let args = KeyValueParser.removeKeyValues args keys

        match args with
        | [] -> Message "No word(s) specified."
        | words ->
            match repeat with
            | true ->
                (0, 0, words)
                |> Seq.unfold repeatFill
                |> String.concat " "
                |> Message
            | false ->
                (0, words)
                |> Seq.unfold randomFill
                |> String.concat " "
                |> Message