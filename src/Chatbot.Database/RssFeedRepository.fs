namespace Database

module RssFeedRepository =

    open DB
    open Types.News

    open Dapper

    type private RssFeedsQuery = {
        category: string
        url: string
    }

    let private mapResult (category: string, feeds: RssFeedsQuery seq) : RssFeeds = {
        Category = category
        Urls = feeds |> List.ofSeq |> List.map (fun f -> f.url)
    }

    let get (category: string) =
        async {
            let pattern = "%" + category + "%"

            let query = """
                SELECT c.category, n.url
                FROM rss_feeds n
                INNER JOIN rss_feed_categories c ON n.category_id = c.category_id
                WHERE c.category LIKE @pattern"""

            try

                let! results = connection.QueryAsync<RssFeedsQuery>(query, {| pattern = pattern |}) |> Async.AwaitTask

                let rssFeeds =
                    results
                    |> Seq.groupBy (fun f -> f.category)
                    |> Seq.map mapResult
                    |> List.ofSeq

                return DatabaseResult.Success rssFeeds
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }