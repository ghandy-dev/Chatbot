namespace Database

module NewsFeedRepository =

    open DB
    open Database.Entities

    open Dapper

    let get (category: string) =
        async {
            let pattern = "%" + category + "%"

            let query = """
                SELECT n.rss_feed_id, n.category_id, n.url
                FROM rss_feeds n
                INNER JOIN rss_feed_categories c ON n.category_id = c.category_id
                WHERE c.category LIKE @pattern"""

            try

                let! results = connection.QueryAsync<Entities.NewsFeed>(query, {| pattern = pattern |}) |> Async.AwaitTask

                let rssFeeds =
                    results
                    |> Seq.groupBy (fun f -> f.category_id)
                    |> Seq.collect snd
                    |> Seq.map (fun r -> r.url)
                    |> List.ofSeq

                return DatabaseResult.Success rssFeeds
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }