[<AutoOpen>]
module Utils

open System

let utcNow() = DateTime.UtcNow

let formatChatMessage (response: string) =
    if response.Length > 500 then
        response[..496] + "..."
    else
        response

let formatTimeSpan (ts: TimeSpan) =
    let formatComponent value =
        if value > 0 then Some (value.ToString()) else None

    let years = if ts.Days >= 365 then Some ((ts.Days / 365).ToString()) else None
    let days = if years.IsSome then formatComponent (ts.Days % 365) else formatComponent ts.Days
    let hours = formatComponent ts.Hours
    let minutes = formatComponent ts.Minutes
    let seconds = formatComponent ts.Seconds

    match years, days, hours, minutes, seconds with
    | Some y, Some d,Some h, _, _ -> sprintf "%sy, %sd, %sh" y d h
    | Some y, None, Some h, _, _ -> sprintf "%sy, %sh" y h
    | Some y, Some d, None, _, _ -> sprintf "%sy, %sd" y d
    | Some y, None , None, _, _ -> sprintf "%sy" y
    | None, Some d, Some h, Some m, _ -> sprintf "%sd, %sh, %sm" d h m
    | None, Some d, None, Some m, _ -> sprintf "%sd, %sm" d m
    | None, Some d, Some h, None, _ -> sprintf "%sd, %sh" d h
    | None, Some d, None, None, _ -> sprintf "%sd" d
    | None, None, Some h, Some m, Some _ -> sprintf "%sh, %sm" h m
    | None, None, Some h, None, Some _ -> sprintf "%sh" h
    | None, None, Some h, Some m, None -> sprintf "%sh, %sm" h m
    | None, None, Some h, None, None -> sprintf "%sh" h
    | None, None, None, Some m, Some s -> sprintf "%sm, %ss" m s
    | None, None, None, Some m, None -> sprintf "%sm" m
    | None, None, None, None, Some s -> sprintf "%ss" s
    | _ -> "0s"


module Seq =

    let tryRandomChoice (source: 'T seq) : 'T option =
        if source |> Seq.length = 0 then
            None
        else
            source |> Seq.randomChoice |> Some

module List =

    let doesNotContain value list = not <| (list |> List.contains value)

    let tryRandomChoice (source: 'T list) : 'T option = source |> Seq.tryRandomChoice

module String =

    let notEmpty = not << String.IsNullOrWhiteSpace

module DateOnly =

    let today() = DateOnly.FromDateTime(utcNow())

    let tryParseExact (s: ReadOnlySpan<char>) (format: ReadOnlySpan<char>) =
        match DateOnly.TryParseExact(s, format) with
        | false, _ -> None
        | true, date -> Some date

module Int32 =

    let tryParse (s: ReadOnlySpan<char>) =
        match System.Int32.TryParse(s) with
        | true, v -> Some v
        | false, _ -> None

    let positive = fun n -> n >= 0 // 🤓 0 isn't positive
    let negative = fun n -> n < 0

module Boolean =

    let tryParse (s: ReadOnlySpan<char>) =
        match System.Boolean.TryParse s with
        | false, _ -> None
        | true, v -> Some v

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

module DateTime =

    let tryParse (s: ReadOnlySpan<char>) =
        match System.DateTime.TryParse s with
        | false, _ -> None
        | true, v -> Some v

    let [<Literal>] DateStringFormat = "dd/MM/yyyy"
    let [<Literal>] TimeStringFormat = "HH:mm:ss"
    let [<Literal>] DateTimeStringFormat = $"dd/MM/yyyy HH:mm:ss"

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
            @"`{1}([\S].*?)`{1}", "$1"              // Inline code
            @"\*{1,2}([\S].*?)\*{1,2}", "$1"        // Bold
            @"-{2,3}", "-"                          // Em/en dash
            @"_{2}([\S].*?)_{2}", "$1"              // Italics
            @"~{2}([\S].*?)~{2}", "$1"              // Strikethrough
            @"#{1,6}\s(.*?)", "$1"                  // Headers
            @"=|-{5,}.*\n", ""                      // Other Headers
            @"\[.*?\][\(](.*?)[\)]", "$1"           // Links
            @"\r\n{1,}", " "                        // CRLF
            @"\n{1,}", " "                          // LF
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

module KeyValueParser =

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

        Regex.Replace(string, pattern, "")
        |> (fun s -> s.Split(" "))
        |> List.ofArray


    let parse (list: string seq) (keys: string seq) =
        let pattern =
            keys
            |> Seq.map patternTemplate
            |> String.concat "|"

        let matches =
            list
            |> Seq.map (fun a -> Regex.Match(a, pattern))
            |> Seq.filter (fun m -> m.Success)
            |> Seq.map (fun m -> m.Value)

        let keyValues = matches |> Seq.choose tryParseKeyValuePair |> Map.ofSeq

        keyValues

module ConcurrentDictionary =

    open System.Collections.Concurrent

    let tryGetValue key (dict: ConcurrentDictionary<_,_>) =
        match dict.TryGetValue key with
        | false, _ -> None
        | true, value -> Some value

