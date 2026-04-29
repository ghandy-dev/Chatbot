namespace Chatbot.Commands

[<AutoOpen>]
module News =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.News

    let news (newsService: INewsService) context =
        asyncResult {
            let maybeCategory =
                if context.MessageArgs |> List.isEmpty then
                    None
                else
                    Some <| (context.MessageArgs |> String.concat " ")

            let! newsItem =
                newsService.GetNews maybeCategory
                |> AsyncResult.mapError InternalError

            let title = newsItem.Title.Text
            let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy, HH:mm")
            let summary = if newsItem.Summary = null then "" else newsItem.Summary.Text
            let link = newsItem.Links |> Seq.tryHead |> Option.bind (fun l -> Some l.Uri.AbsoluteUri) |? ""

            return [ Message $"{date} {title} {summary} {link}" ]
        }
