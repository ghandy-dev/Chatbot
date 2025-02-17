namespace Commands

[<AutoOpen>]
module UrbanDictionary =

    open System.Text.RegularExpressions

    open UrbanDictionary.Api

    let urban args =
        async {
            let getTerm =
                match args with
                | [] -> random ()
                | _ ->
                    let query = args |> String.concat " "
                    search query

            match! getTerm with
            | Error err -> return Message err
            | Ok terms ->
                match terms with
                | [] -> return Message "No definition found"
                | term :: _ ->
                    let definition =
                        [ @"[\[\]]", "" ; @"(\r\n|\n)", " " ]
                        |> List.fold (fun acc (pattern, replacement) -> Regex.Replace(acc, pattern, replacement)) term.Definition

                    return Message $"{term.Permalink} (+{term.ThumbsUp}/-{term.ThumbsDown}) {term.Word}: {definition}"
        }
