namespace Commands

[<AutoOpen>]
module News =

    open News.Api

    open FsToolkit.ErrorHandling

    let news args =
        asyncResult {
            let maybeCategory =
                if args |> List.isEmpty then
                    None
                else
                    Some <| (args |> String.concat " ")

            let! newsItem = getNews maybeCategory
            let title = newsItem.Title.Text
            let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy, HH:mm")
            let summary = if newsItem.Summary = null then "" else newsItem.Summary.Text
            let link = newsItem.Links |> Seq.tryHead |> Option.bind (fun l -> Some l.Uri.AbsoluteUri) |? ""
            return Message $"{date} {title} {summary} {link}"
        }
