module KeyValueParser

open System.Text.RegularExpressions

type KeyValueParserResult = {
    KeyValues: Map<string, string>
    Input: string list
}

let private patternTemplate = sprintf @"%s:(("".*?"")|(\S+))"

let private tryParseKeyValuePair (string: string) =
    string.Split(":")
    |> function
        | [| key ; value |] ->
            let value = if value.StartsWith("\"") then value.Trim('"') else value
            Some(key, value)
        | _ -> None

let private removeKeyValues (list: string seq) (keys: string seq)  =
    let string = list |> String.concat " "

    let pattern =
        keys
        |> Seq.map patternTemplate
        |> String.concat "|"

    Regex.Replace(string, pattern, "").Split(" ", System.StringSplitOptions.RemoveEmptyEntries)
    |> List.ofArray

let parse (input: string seq) (keys: string seq) =
    let s = input |> String.concat " "

    let patterns =
        keys
        |> Seq.map patternTemplate

    let keyValues =
        patterns
        |> Seq.choose (fun p ->
            let m = Regex.Match(s, p)
            if m.Success then
                m.Value |> tryParseKeyValuePair
            else
                None
        )
        |> Map.ofSeq

    let input = removeKeyValues input keys

    { KeyValues = keyValues
      Input = input }
