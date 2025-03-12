module KeyValueParser

open System.Text.RegularExpressions

let private patternTemplate = sprintf @"%s:(("".*?"")|(\S+))"

let tryParseKeyValuePair (string: string) =
    string.Split(":")
    |> function
        | [| key ; value |] ->
            let value = if value.StartsWith("\"") then value.Trim('"') else value
            Some(key, value)
        | _ -> None

let removeKeyValues (list: string seq) (keys: string seq)  =
    let string = list |> String.concat " "

    let pattern =
        keys
        |> Seq.map patternTemplate
        |> String.concat "|"

    Regex.Replace(string, pattern, "").Split(" ", System.StringSplitOptions.RemoveEmptyEntries)
    |> List.ofArray

let parse (list: string seq) (keys: string seq) =
    let s = list |> String.concat " "

    let patterns =
        keys
        |> Seq.map patternTemplate

    let matches =
        patterns
        |> Seq.choose (fun p ->
            let m = Regex.Match(s, p)
            if m.Success then Some m.Value else None
        )

    let keyValues = matches |> Seq.choose tryParseKeyValuePair |> Map.ofSeq

    keyValues