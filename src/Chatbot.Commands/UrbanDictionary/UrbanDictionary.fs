namespace Commands

[<AutoOpen>]
module UrbanDictionary =

    open UrbanDictionary.Api

    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    let urban context =
        asyncResult {
            let getTerm =
                match context.Args with
                | [] -> random ()
                | args ->
                    let query = args |> String.concat " "
                    search query

            let! terms = getTerm |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "UrbanDictionary")

            match terms with
            | [] -> return Message "No definition found!"
            | term :: _ ->
                let definition =
                    [ @"[\[\]]", "" ; @"(\r\n|\n)", " " ]
                    |> List.fold (fun acc (pattern, replacement) -> Regex.Replace(acc, pattern, replacement)) term.Definition

                return Message $"{term.Permalink} (+{term.ThumbsUp}/-{term.ThumbsDown}) {term.Word}: {definition}"
        }
