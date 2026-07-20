namespace Chatbot.Commands

[<AutoOpen>]
module News =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.News
    open Chatbot.Common

    let keys = [ "category" ; "country" ]

    let private search (newsService: INewsService) query country =
        asyncResult {
            let! newsItem =
                newsService.Search query country
                |> AsyncResult.mapError InternalError

            let source = newsItem.SourceFeed.Title.Text
            let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy")
            let title = newsItem.Title.Text
            let summary = if newsItem.Summary = null then "" else $"| {newsItem.Summary.Text}" |> removeHtmlTags |> htmlDecode

            return [ Message $"{date}, {source}: {title} {summary}" ]
        }

    let private get (newsService: INewsService) categoryOpt =
        asyncResult {
            let! newsItem =
                newsService.Get categoryOpt
                |> AsyncResult.mapError InternalError

            let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy")
            let title = newsItem.Title.Text
            let summary = if newsItem.Summary = null then "" else $"| {newsItem.Summary.Text}" |> removeHtmlTags |> htmlDecode
            let link = newsItem.Links |> Seq.tryHead |> Option.bind (fun l -> Some l.Uri.AbsoluteUri) |? ""

            return [ Message $"{date}, {title} {summary} {link}" ]
        }

    let news (newsService: INewsService) context =
        asyncResult {
            let kvp = KeyValueParser.parse context.MessageArgs keys

            let categoryOpt = kvp.KeyValues |> Map.tryFind "category"
            let country = kvp.KeyValues |> Map.tryFind "country" |> Option.bind (fun c -> Globalisation.countryCodes |> Dict.tryGetValue c) |> Option.defaultValue "GB"

            match kvp.Input with
            | "search" :: query -> return! search newsService (query |> String.join " ") country
            | _ -> return! get newsService categoryOpt
        }