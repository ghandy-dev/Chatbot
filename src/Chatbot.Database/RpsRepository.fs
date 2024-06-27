namespace Chatbot.Database

module RpsRepository =

    open DB
    open Dapper.FSharp.SQLite
    open Types

    let private mapRpsStatsEntity (entity: Entities.RpsStats) : RpsStats = {
        UserId = entity.user_id
        Score = entity.score
        TotalMoves = entity.total_moves
        Wins = entity.wins
        Losses = entity.losses
    }

    let private mapRpsStats (record: RpsStats) : Entities.RpsStats = {
        rps_stats_id = 0
        user_id = record.UserId
        score = record.Score
        total_moves = record.TotalMoves
        wins = record.Wins
        losses = record.Losses
    }

    let getById (userId: int) =
        async {
            let! stats =
                select {
                    for row in rpsStats do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<Entities.RpsStats>
                |> Async.AwaitTask

            return stats |> Seq.map mapRpsStatsEntity |> Seq.tryExactlyOne
        }

    let add (stats: RpsStats) =
        async {
            let newStats = mapRpsStats stats

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
                return DatabaseResult.Failure ex
        }

    let update (stats: RpsStats) =
        async {
            let updatedStats = mapRpsStats stats

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
                return DatabaseResult.Failure ex
        }
