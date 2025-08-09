namespace Database

module RpsRepository =

    open Dapper.FSharp.SQLite

    open Database.Models
    open Database.Entities
    open DB

    let get (userId: int) =
        async {
            let! stats =
                select {
                    for row in rpsStats do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<Entities.RpsStats>
                |> Async.AwaitTask

            return
                stats
                |> Seq.map (fun r -> {
                    UserId = r.user_id
                    Score = r.score
                    TotalMoves = r.total_moves
                    Wins = r.wins
                    Losses = r.losses
                })
                |> Seq.tryExactlyOne
        }

    let add (stats: Models.RpsStats) =
        async {
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

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }

    let update (stats: Models.RpsStats) =
        async {
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
                    |> connection.UpdateAsync<Entities.RpsStats>
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
