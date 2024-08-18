namespace Chatbot.Commands

[<AutoOpen>]
module News =

    open Chatbot.Commands.Api.News

    let news args =
        async {
            let! result =
                match args with
                | [] -> getNews None
                | _ ->
                    let category = args |> String.concat " "
                    getNews (Some category)

            match result with
            | Error err -> return Error err
            | Ok newsItem ->
                let title = newsItem.Title.Text
                let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy, HH:mm")
                let summary = newsItem.Summary.Text
                let link =
                    match newsItem.Links |> List.ofSeq with
                    | [] -> ""
                    | l :: _ -> l.Uri.AbsoluteUri

                return Ok <| Message $"{date} {title} {summary} {link}"
        }
