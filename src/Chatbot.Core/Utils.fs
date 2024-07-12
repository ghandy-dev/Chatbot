module Utils

module Int32 =

    let tryParse (s: string) =
        match System.Int32.TryParse(s) with
        | true, v -> Some v
        | false, _ -> None

module Boolean =

    let tryParseBit =
        function
        | "1" -> Some true
        | "0"
        | "-1" -> Some false
        | _ -> None

    let parseBit =
        function
        | "0" -> false
        | _ -> true

module Text =

    open System.Text.RegularExpressions

    let formatString (format: string) (args: string list) =
        let pattern = @"\{(\d+)\}"
        Regex.Replace(format, pattern, fun m ->
            let index = int m.Groups.[1].Value
            args.[index])

    let stripMarkdownTags content =
        let patterns = [
            @"`{3}", ""                             // Code Blocks
            @"\*{1,2}([/w/s+].*?)\*{1,2}", "$1"     // Bold
            @"_{2}([/w/s+].*?)_{2}", "$1"           // Italics
            @"~{2}([/w/s+].*?)~{2}", "$1"           // Strikethrough
            @"#{1,6}\s(.*?)", "$1"                  // Headers
            @"=|-{5,}.*\n", ""                      // Other Headers
            @"\[.*?\][\(](.*?)[\)]", "$1"           // Links
            @"\n{1,}", " "
        ]

        let stripped =
            patterns
            |> List.fold (fun acc (pattern, replacement) ->
                Regex.Replace(acc, pattern, replacement, RegexOptions.Multiline)
            ) content

        stripped

module Map =

    let private add = fun acc key value -> Map.add key value acc

    let merge (a: Map<'a, 'b>) (b: Map<'a, 'b>) =

        if a.Count < b.Count then
            Map.fold add b a
        else
            Map.fold add a b

    let mergeInto (into: Map<'a, 'b>) (from: Map<'a, 'b>) = Map.fold add into from

    let mergeFrom = fun into from -> mergeInto from into

module Array =

    let swap (array: array<'a>) n k =
        let copy = array[n]
        array[n] <- array[k]
        array[k] <- copy

module List =

    let doesNotContain value list = not <| (list |> List.contains value)

module KeyValueParser =

    open System.Text.RegularExpressions

    let private patternTemplate = sprintf @"%s:""*(\w+)""*\s*"
    let private captureAllPattern = @"(\w+):""*(\w+)""*\s*"

    let parseKeyValuePair (string: string) =
        string.Split(":")
        |> function
            | [| key ; value |] ->
                let value = if value.StartsWith("\"") then value.Trim('"') else value
                Some(key, value)
            | _ -> None

    let private removeKeyValuePairs list pattern =
        let string = list |> String.concat " "

        Regex.Replace(string, pattern, "")
        |> (fun s -> s.Split(" "))
        |> List.ofArray

    let parseKeyValuePairs list (keys: string seq option) =
        let pattern =
            match keys with
            | None -> captureAllPattern
            | Some keys ->
                keys
                |> Seq.map patternTemplate
                |> String.concat "|"

        let matches =
            list
            |> List.map (fun a -> Regex.Match(a, pattern))
            |> List.filter (fun m -> m.Success)
            |> List.map (fun m -> m.Value)

        let map = matches |> List.choose parseKeyValuePair |> Map.ofList
        let newList = removeKeyValuePairs list pattern

        (newList, map)
