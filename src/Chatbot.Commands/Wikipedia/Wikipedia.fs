namespace Commands

[<AutoOpen>]
module Wikipedia =

    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open CommandError
    open Wikipedia.Api

    let wiki args =
        asyncResult {
            match args with
            | [] -> return! invalidArgs "No input provided."
            | input ->
                let query = String.concat " " input
                let! pages = getWikiResults query |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")
                let htmlTagPattern = "<.*?>"

                return
                    match pages.Pages with
                    | [] -> Message "No wikipedia page found!"
                    | page :: _ ->
                        let key = page.Key
                        let excerpt = Regex.Replace(page.Excerpt, htmlTagPattern, "")
                        Message $"https://en.wikipedia.org/wiki/{key} {excerpt}"
        }

    let onThisDay args =
        asyncResult {
            let! otds = getOnThisDay () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")

            return
                match otds with
                | [] -> Message """No events for "On this day" """
                | os ->
                    os
                    |> Seq.randomChoice
                    |> fun otd ->
                        let year = otd.Year
                        let text = otd.Text
                        let links = otd.Pages |> Seq.map _.ContentUrls.Desktop.Page |> strJoin ", "

                        Message $"{year} {text} ({links})"
        }

    let wikiNews args =
        asyncResult {
            let! news = getNews () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")
            let htmlTagPattern = "<.*?>"

            return
                match news with
                | [] -> Message "No news articles"
                | ns ->
                    ns
                    |> Seq.randomChoice
                    |> fun n ->
                        let story = Regex.Replace(n.Story, htmlTagPattern, "")
                        let links = n.Links |> Seq.map  _.ContentUrls.Desktop.Page |> strJoin ", "

                        Message $"{story} ({links})"
        }

    let didYouKnow args =
        asyncResult {
            let! dyks = getDidYouKnow () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")
            let wikiPagePattern = "https:\/\/en.wikipedia.org\/wiki\/\w+"

            return
                match dyks with
                | [] -> Message """No "Did you know" articles"""
                | ds ->
                    ds
                    |> Seq.randomChoice
                    |> fun dyk ->
                        let text = dyk.Text
                        let links =
                            Regex.Matches(dyk.Html, wikiPagePattern)
                            |> Seq.map _.Value
                            |> strJoin ", "

                        Message $"{text} ({links})"
        }