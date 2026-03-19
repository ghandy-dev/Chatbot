namespace Commands

[<AutoOpen>]
module Trivia =

    open System
    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open CommandError
    open Parsing
    open Trivia

    let private keys = [ "count" ; "exclude" ; "include" ; "hints" ]

    let private replacePattern = "[A-Za-z0-9\(\)\[\]]"

    let trivia context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidUsage "Trivia can only be used in channels"
            | Channel channel ->
                match context.Args with
                | "stop" :: _ -> return BotCommand.stopTrivia channel.Channel
                | args ->
                    let kvp = KeyValueParser.parse args keys

                    let count = kvp.KeyValues.TryFind "count" |> Option.bind tryParseInt |> Option.map (fun c -> Math.Clamp(c, 1, 10)) |? 1
                    let excludeCategories = kvp.KeyValues.TryFind "exclude" |> Option.map _.Split(",", System.StringSplitOptions.TrimEntries)
                    let includeCategories = kvp.KeyValues.TryFind "include" |> Option.map _.Split(",", System.StringSplitOptions.TrimEntries)
                    let hints = kvp.KeyValues.TryFind "hints" |> Option.bind tryParseBoolean |? true

                    let! questions = Api.getQuestions count excludeCategories includeCategories |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Trivia")

                    match questions with
                    | [] -> return Message $"No questions found for selected categories"
                    | questions ->
                        let questions =
                            questions |> List.map (fun q ->
                                let clue = Regex.Replace(q.Answer, replacePattern, "_")

                                {
                                   Question = $"""{q.Question} ({clue})"""
                                   Answer = q.Answer
                                   Hints = [ q.Hint1 ; q.Hint2 ] |> List.choose id
                                   Categories = q.Categories
                                   Category = q.Category
                                })

                        let triviaConfig = {
                            Questions = questions
                            Count = questions.Length
                            Categories = questions |> List.map (fun q -> q.Category)
                            Channel = channel.Channel
                            UseHints = hints
                            Timestamp = DateTime.MaxValue
                            HintsSent = []
                        }

                        return BotCommand.startTrivia triviaConfig
        }