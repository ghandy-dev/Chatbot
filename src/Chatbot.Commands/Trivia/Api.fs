namespace Trivia

module Api =

    open Types
    open Http

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

            match! getFromJsonAsync<Question list> url with
            | Error (err, _) ->
                Logging.error err (new exn(err))
                return None
            | Ok questions ->
                return Some questions
        }
