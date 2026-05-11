namespace Chatbot.Commands

[<AutoOpen>]
module News =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.News

    let news (newsService: INewsService) context =
        asyncResult {
            let categoryOpt =
                match context.MessageArgs with
                | [] -> None
                | args -> Some (args |> String.join " ")

            let! newsItem =
                newsService.GetNews categoryOpt
                |> AsyncResult.mapError InternalError

            let title = newsItem.Title.Text
            let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy")
            let summary = if newsItem.Summary = null then "" else newsItem.Summary.Text
            let link = newsItem.Links |> Seq.tryHead |> Option.bind (fun l -> Some l.Uri.AbsoluteUri) |? ""

            return [ Message $"{date}, {title} {summary} {link}" ]
        }
