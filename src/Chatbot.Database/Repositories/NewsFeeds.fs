namespace Chatbot.Database

module NewsFeeds =

    open Microsoft.Data.Sqlite

    open Dapper

    open Chatbot.Database.Entities
    open Db

    let get (db: Database) (category: string) =
        async {
            let pattern = "%" + category + "%"

            let query = """
                SELECT n.rss_feed_id, n.category_id, n.url
                FROM rss_feeds n
                INNER JOIN rss_feed_categories c ON n.category_id = c.category_id
                WHERE c.category LIKE @pattern"""

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! results = connection.QueryAsync<Entities.NewsFeed>(query, {| pattern = pattern |}) |> Async.AwaitTask

                let rssFeeds =
                    results
                    |> Seq.groupBy (fun f -> f.category_id)
                    |> Seq.collect snd
                    |> Seq.map (fun r -> r.url)
                    |> List.ofSeq

                return DatabaseResult.Success rssFeeds
            with ex ->
                return DatabaseResult.Failure
        }