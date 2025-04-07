module DateTime

open System
open Microsoft.Recognizers.Text
open Microsoft.Recognizers.Text.DateTime

let tryParse (s: ReadOnlySpan<char>) =
    match System.DateTime.TryParse s with
    | false, _ -> None
    | true, v -> Some v

let tryParseNaturalLanguageDateTime (query: string) =
    let culture = Culture.English

    let results = DateTimeRecognizer.RecognizeDateTime(query, culture)

    match results |> List.ofSeq with
    | [] -> None
    | result :: _ ->
        let resolution = result.Resolution

        match resolution.TryGetValue("values") with
        | false, _ -> None
        | true, values ->
            match values with
            | :? System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> as v ->
                if v.Count > 0 then
                    let obj = v.[0]

                    match obj.TryGetValue("value"), obj.TryGetValue("type"), obj.TryGetValue("timex") with
                    | (true, _), (true, "date"), (true, d) ->
                        let date = DateTime.Parse(d)
                        let difference = date - now()
                        let datetime = now().AddDays(difference.Days)
                        Some (datetime, result.Start, result.End)
                    | (true, t), (true, "time"), (true, _) ->
                        let time = TimeOnly.Parse(t)
                        let datetime = now().Date.AddSeconds(time.ToTimeSpan().TotalSeconds)
                        Some (datetime, result.Start, result.End)
                    | (true, _), (true, "datetime"), (true, dt) ->
                        tryParse dt |> Option.bind (fun dt -> Some (dt, result.Start, result.End))
                    | _, _, _ -> None
                else
                    None
            | _ -> None