namespace Chatbot.Commands

[<AutoOpen>]
module UrbanDictionary =

    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.UrbanDictionary

    let urban (urbanDictionaryService: UrbanDictionaryService) context =
        asyncResult {
            let getTerm =
                match context.MessageArgs with
                | [] -> urbanDictionaryService.Random ()
                | args ->
                    let query = args |> String.concat " "
                    urbanDictionaryService.Search query

            let! terms = getTerm |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "UrbanDictionary")

            match terms with
            | [] -> return [ Message "No definition found!" ]
            | term :: _ ->
                let definition =
                    [ @"[\[\]]", "" ; @"(\r\n|\n)", " " ]
                    |> List.fold (fun acc (pattern, replacement) -> Regex.Replace(acc, pattern, replacement)) term.Definition

                return [ Message $"{term.Permalink} {term.Word}: {definition}" ]
        }
