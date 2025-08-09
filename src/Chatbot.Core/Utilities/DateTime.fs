module DateTime

open System

open FSharpPlus
open Microsoft.Recognizers.Text
open Microsoft.Recognizers.Text.DateTime

let tryParseNaturalLanguageDateTime (query: string) =
    let culture = Culture.English

    let results = DateTimeRecognizer.RecognizeDateTime(query, culture)

    match results |> List.ofSeq with
    | [] -> None
    | result :: _ ->
        let resolution = result.Resolution

        match resolution |> Dict.tryGetValue "values" with
        | None -> None
        | Some values ->
            match values with
            | :? System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> as v ->
                if v.Count > 0 then
                    let obj = v.[0]

                    let value = obj |> Dict.tryGetValue "value"
                    let ``type`` = obj |> Dict.tryGetValue "type"
                    let timex = obj |> Dict.tryGetValue "timex"

                    match value, ``type``, timex  with
                    | _, Some "date", Some d ->
                        let date = DateTime.Parse(d)
                        let difference = date - now()
                        let datetime = now().AddDays(difference.Days)
                        Some (datetime, result.Start, result.End)
                    | Some t, Some "time", Some _ ->
                        let time = TimeOnly.Parse(t)
                        let datetime = now().Date.AddSeconds(time.ToTimeSpan().TotalSeconds)
                        Some (datetime, result.Start, result.End)
                    | _, Some "datetime", Some dt ->
                        let date = DateTime.Parse(dt)
                        Some (date, result.Start, result.End)
                    | _, _, _ -> None
                else
                    None
            | _ -> None