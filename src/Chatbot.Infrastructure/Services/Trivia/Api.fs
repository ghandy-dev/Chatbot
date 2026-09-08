namespace Chatbot.Infrastructure.Services

open System

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.Trivia.Types

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
                    if excludeCategories |> List.isEmpty then None else Some (excludeCategories |> String.join "," |> fun cs -> "exclude", cs)
                    if includeCategories |> List.isEmpty then None else Some (includeCategories |> String.join "," |> fun cs -> "include", cs)
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