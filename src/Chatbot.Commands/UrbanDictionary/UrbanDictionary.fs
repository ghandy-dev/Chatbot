namespace Chatbot.Commands.UrbanDictionary

[<AutoOpen>]
module Reddit =

    open Api
    open Chatbot.Commands
    open Utils.KeyValueParser

    open System.Text.RegularExpressions

    let private keys = [ "random" ]

    let pattern = @"[\[\]]|\n"

    let urban args =
        async {
            let (_, map) = parseKeyValuePairs args (Some keys)

            let f =
                if map.ContainsKey "random" then
                    random()
                else
                    let query = args |> String.concat " "
                    search query

            match! f with
            | Error err -> return Error err
            | Ok terms ->
                match terms with
                | [] -> return Ok <| Message "No definition found"
                | t :: _ ->
                    let definition = Regex.Replace(t.Definition, pattern, "")
                    return Ok <| Message $"{t.Permalink} (+{t.ThumbsUp}/-{t.ThumbsDown}) {t.Word}: {definition}"

        }
