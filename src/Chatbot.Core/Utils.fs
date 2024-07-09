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

    let private parseKeyValuePair (m: Match) =
        m.Value.Split(":")
        |> (fun pair ->
            match pair with
            | [| key ; value |] ->

                let value = if value.StartsWith("\"") then value.Trim('"') else value
                Some(key, value)
            | _ -> None
        )

    let parseKeyValuePairs (input: string) =
        let pattern = @"(\w+):([\w]+|\""(.*?)\"")"
        let regex = new Regex(pattern)
        let matches = regex.Matches(input)
        matches |> Seq.choose parseKeyValuePair |> Map.ofSeq


module Map =

    let private add = fun acc key value -> Map.add key value acc

    let merge (a: Map<'a, 'b>) (b: Map<'a, 'b>) =

        if a.Count < b.Count then
            Map.fold add b a
        else
            Map.fold add a b

    let mergeInto (into: Map<'a, 'b>) (from: Map<'a, 'b>) = Map.fold add into from

module Array =

    let swap (array: array<'a>) n k =
        let copy = array[n]
        array[n] <- array[k]
        array[k] <- copy

module List =

    let doesNotContain value list = not <| (list |> List.contains value)
