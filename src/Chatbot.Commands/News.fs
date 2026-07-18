namespace Chatbot.Commands

[<AutoOpen>]
module News =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.News
    open Chatbot.Common

    let keys = [ "category" ]

    let news (newsService: INewsService) context =
        asyncResult {
            let kvp = KeyValueParser.parse context.MessageArgs keys
            let args = kvp.Input

            let categoryOpt = kvp.KeyValues |> Map.tryFind "category"

            let! newsItem =
                match categoryOpt, args with
                | None, _ :: _ ->
                    let query = args |> String.join " "
                    newsService.Search  query
                | None, [] -> newsService.Get None
                | Some _, _ -> newsService.Get categoryOpt
                |> AsyncResult.mapError InternalError

            let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy")
            let title = newsItem.Title.Text
            let summary = if newsItem.Summary = null then "" else $"| {newsItem.Summary.Text}" |> removeHtmlTags |> htmlDecode
            let link = newsItem.Links |> Seq.tryHead |> Option.bind (fun l -> Some l.Uri.AbsoluteUri) |? ""

            return [ Message $"{date}, {title} {summary} {link}" ]
        }
