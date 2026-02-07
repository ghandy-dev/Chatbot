namespace Trivia

module Api =

    open System.Net.Http

    open FsToolkit.ErrorHandling

    open Http
    open Types

    let [<Literal>] private ApiUrl = "https://api.gazatu.xyz/trivia/questions"

    let private getQuestionsUrl (count: string) (excludeCategories: string option) (includeCategories: string option) =
        let queryParams =
            [
                Some $"count={count}"
                excludeCategories |> Option.map (sprintf "exclude=%s")
                includeCategories |> Option.map (sprintf "include=%s")
            ]
            |> List.choose id
            |> String.concat "&"

        $"{ApiUrl}?{queryParams}"

    let getQuestions (count: int) (excludeCategories: string array option) (includeCategories: string array option) =
        let maybeConcat v = v |> Option.map (fun s -> $"""[{s |> String.concat ","}]""")

        async {
            let url =
                getQuestionsUrl
                    $"{count}"
                    (maybeConcat excludeCategories)
                    (maybeConcat includeCategories)

            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Question list>
                |> Result.map(List.map (fun q -> { q with Answer = q.Answer.Trim() }))
                |> Result.mapError _.StatusCode
        }
