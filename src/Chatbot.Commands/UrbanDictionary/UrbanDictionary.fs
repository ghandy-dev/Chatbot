namespace Chatbot.Commands.UrbanDictionary

[<AutoOpen>]
module Reddit =

    open Api
    open Chatbot.Commands

    open System.Text.RegularExpressions

    let pattern = @"[\[\]]|\n"

    let urban args =
        async {
            let getTerm =
                match args with
                | [] -> random()
                | _ ->
                    let query = args |> String.concat " "
                    search query

            match! getTerm with
            | Error err -> return Message err
            | Ok terms ->
                match terms with
                | [] -> return Message "No definition found"
                | t :: _ ->
                    let definition = Regex.Replace(t.Definition, pattern, "")
                    return Message $"{t.Permalink} (+{t.ThumbsUp}/-{t.ThumbsDown}) {t.Word}: {definition}"

        }
