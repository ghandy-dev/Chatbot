namespace Commands

[<AutoOpen>]
module News =

    open News.Api

    let news args =
        async {
            let! result =
                match args with
                | [] -> getNews None
                | _ ->
                    let category = args |> String.concat " "
                    getNews (Some category)

            match result with
            | Error err -> return Message err
            | Ok newsItem ->
                let title = newsItem.Title.Text
                let date = newsItem.PublishDate.UtcDateTime.ToString("dd MMM yyyy, HH:mm")
                let summary = if newsItem.Summary = null then "" else newsItem.Summary.Text
                let link = newsItem.Links |> Seq.tryHead |> Option.bind (fun l -> Some l.Uri.AbsoluteUri) |?? ""
                return Message $"{date} {title} {summary} {link}"
        }
