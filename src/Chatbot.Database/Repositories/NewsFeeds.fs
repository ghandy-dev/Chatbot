namespace Chatbot.Database

[<RequireQualifiedAccess>]
module NewsFeeds =

    open Microsoft.Data.Sqlite

    open Dapper

    open Chatbot.Database.Db
    open Chatbot.Database.DbModels

    let getFeeds (db: Database) (category: string) =
        async {
            let pattern = "%" + category + "%"

            let query =
                """
                SELECT n.rss_feed_id, n.category_id, n.url
                FROM rss_feeds n
                INNER JOIN rss_feed_categories c ON n.category_id = c.category_id
                WHERE c.category LIKE @pattern
                """

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! results = connection.QueryAsync<DbNewsFeed>(query, {| pattern = pattern |}) |> Async.AwaitTask

                let rssFeeds =
                    results
                    |> Seq.map (fun r -> r.url)
                    |> List.ofSeq

                return rssFeeds
            with ex ->
                return []
        }