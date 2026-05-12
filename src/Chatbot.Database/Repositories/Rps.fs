namespace Chatbot.Database

[<RequireQualifiedAccess>]
module Rps =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Chatbot.Database.Db
    open Chatbot.Database.DbModels
    open Chatbot.Database.Types

    let private toRpsStats (dbStats: DbRpsStats) : RpsStats =
        {
            UserId = dbStats.user_id
            Score = dbStats.score
            TotalMoves = dbStats.total_moves
            Wins = dbStats.wins
            Losses = dbStats.losses
        }

    let get (db: Database) (userId: int) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! stats =
                select {
                    for row in rpsStats do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<DbRpsStats>
                |> Async.AwaitTask

            return
                stats
                |> Seq.tryHead
                |> Option.map toRpsStats
        }

    let add (db: Database) (stats: RpsStats) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let newStats = {
                rps_stats_id = 0
                user_id = stats.UserId
                score = stats.Score
                total_moves = stats.TotalMoves
                wins = stats.Wins
                losses = stats.Losses
            }

            try
                let! rowsAffected =
                    insert {
                        for row in rpsStats do
                            value newStats
                            excludeColumn row.rps_stats_id
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let update (db: Database) (stats: RpsStats) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let updatedStats = {
                rps_stats_id = 0
                user_id = stats.UserId
                score = stats.Score
                total_moves = stats.TotalMoves
                wins = stats.Wins
                losses = stats.Losses
            }

            try
                let! rowsAffected =
                    update {
                        for row in rpsStats do
                            set updatedStats
                            where (row.user_id = updatedStats.user_id)
                            excludeColumn updatedStats.rps_stats_id
                    }
                    |> connection.UpdateAsync<DbRpsStats>
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
        }
