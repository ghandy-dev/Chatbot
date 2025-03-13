namespace Commands

[<AutoOpen>]
module Trivia =

    open Trivia

    let keys = [ "count" ; "exclude" ; "include" ; "hints" ]

    let trivia args context =
        match context.Source with
        | Whisper _ -> async { return Message "Trivia can only be used in channels" }
        | Channel channel ->
            match args with
            | "stop" :: _ -> async { return BotAction (StopTrivia channel.Channel, None) }
            | _ ->
                let options = KeyValueParser.parse args keys

                let count = options |> Map.tryFind "count" |> Option.bind (fun s -> Int32.tryParse s) |> Option.map (fun c -> max c 10) |?? 1
                let excludeCategories = options |> Map.tryFind "exclude" |> Option.map _.Split(",", System.StringSplitOptions.TrimEntries)
                let includeCategories = options |> Map.tryFind "include" |> Option.map _.Split(",", System.StringSplitOptions.TrimEntries)
                let hints = options |> Map.tryFind "hints" |> Option.bind (fun s -> Boolean.tryParse s) |?? true

                async {
                    match! Api.getQuestions count excludeCategories includeCategories with
                    | None -> return Message "Failed to start trivia, check logs"
                    | Some [] -> return Message $"No questions found for selected categories"
                    | Some questions ->
                        let triviaConfig = {
                            Questions = (questions |> List.map (fun q -> {
                                Question = q.Question
                                Answer = q.Answer
                                Hints = [ q.Hint1 ; q.Hint2 ] |> List.choose id
                                Categories = q.Categories
                                Category = q.Category
                            }))
                            Categories = questions |> List.map (fun q -> q.Category)
                            Channel = channel.Channel
                            UseHints = hints
                        }

                        return BotAction (StartTrivia triviaConfig, None)
                }