namespace Chatbot.Core.Services

module Trivia =

    open System

    module Types =

        type Question = {
            Id: string
            Categories: string array
            Question: string
            Answer: string
            Hint1: string option
            Hint2: string option
            Submitter: string option
            CreatedAt: DateTime
            Category: string
        }

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types
    open Types

    type ITriviaService =
        abstract member GetQuestions: count: int -> excludeCategories: string list -> includeCategories: string list -> Async<Result<Question list, int>>

    module TriviaService =

        let create env =

            let apiUrl = "https://api.gazatu.xyz/trivia/questions"

            let httpClient = env.HttpClient

            let getQuestionsUrl (count: string) (excludeCategories: string list) (includeCategories: string list) =
                UrlBuilder.buildUrl
                    apiUrl
                    ([
                        Some ("count", count)
                        if excludeCategories |> List.isEmpty then None else Some (excludeCategories |> String.concat "," |> fun cs -> "exclude", cs)
                        if includeCategories |> List.isEmpty then None else Some (includeCategories |> String.concat "," |> fun cs -> "include", cs)
                    ]
                    |> List.choose id)

            let getQuestions (count: int) (excludeCategories: string list) (includeCategories: string list) =
                async {
                    let url =
                        getQuestionsUrl
                            $"{count}"
                            excludeCategories
                            includeCategories

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Question list>
                        |> Result.map(List.map (fun q -> { q with Answer = q.Answer.Trim() }))
                        |> Result.mapError _.StatusCode
                }

            {
                new ITriviaService with
                    member _.GetQuestions count excludeCategories includeCategories = getQuestions count excludeCategories includeCategories
            }