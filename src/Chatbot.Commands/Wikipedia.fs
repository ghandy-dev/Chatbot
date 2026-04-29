namespace Chatbot.Commands

[<AutoOpen>]
module Wikipedia =

    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Wikipedia

    let wiki (wikiService: WikipediaService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No input provided."
            | input ->
                let query = String.concat " " input
                let! pages = wikiService.GetWikiResults query |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")
                let htmlTagPattern = "<.*?>"

                return!
                    match pages.Pages with
                    | [] -> Ok [ Message "No wikipedia page found!" ]
                    | page :: _ ->
                        let key = page.Key
                        let excerpt = Regex.Replace(page.Excerpt, htmlTagPattern, "")
                        Ok [ Message $"https://en.wikipedia.org/wiki/{key} {excerpt}" ]
        }

    let onThisDay (wikiService: WikipediaService) context =
        asyncResult {
            let! otds = wikiService.GetOnThisDay () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")

            return
                match otds with
                | [] -> [ Message """No events for "On this day" """ ]
                | os ->
                    os
                    |> Seq.randomChoice
                    |> fun otd ->
                        let today = utcNow()
                        let year = otd.Year
                        let text = otd.Text
                        let links = otd.Pages |> Seq.map _.ContentUrls.Desktop.Page |> strJoin ", "

                        [ Message $"""{today.ToString("dd MMM")} {year}, {text} ({links})""" ]
        }

    let wikiNews (wikiService: WikipediaService) context =
        asyncResult {
            let! news = wikiService.GetNews () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")
            let htmlTagPattern = "<.*?>"

            return
                match news with
                | [] -> [ Message "No news articles" ]
                | ns ->
                    ns
                    |> Seq.randomChoice
                    |> fun n ->
                        let story = Regex.Replace(n.Story, htmlTagPattern, "")
                        let links = n.Links |> Seq.map  _.ContentUrls.Desktop.Page |> strJoin ", "

                        [ Message $"{story} ({links})" ]
        }

    let didYouKnow (wikiService: WikipediaService) context =

        asyncResult {
            let! dyks = wikiService.GetDidYouKnow () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")
            let referenceLinkPattern = "(?:href\=\")(.*?)\""

            return
                match dyks with
                | [] -> [ Message """No "Did you know" articles""" ]
                | ds ->
                    ds
                    |> Seq.randomChoice
                    |> fun dyk ->
                        let text = dyk.Text
                        let links =
                            Regex.Matches(dyk.Html, referenceLinkPattern)
                            |> Seq.map _.Groups.[1].Value
                            |> strJoin ", "

                        [ Message $"{text} ({links})" ]
        }