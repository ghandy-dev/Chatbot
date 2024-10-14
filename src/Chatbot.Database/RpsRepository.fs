namespace Database

module RpsRepository =

    open DB
    open Types.RockPaperScissors

    open Dapper.FSharp.SQLite

    let private mapEntity (entity: Entities.RpsStats) : RpsStats = {
        UserId = entity.user_id
        Score = entity.score
        TotalMoves = entity.total_moves
        Wins = entity.wins
        Losses = entity.losses
    }

    let private mapRecord (record: RpsStats) : Entities.RpsStats = {
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

            return stats |> Seq.map mapEntity |> Seq.tryExactlyOne
        }

    let add (stats: RpsStats) =
        async {
            let newStats = mapRecord stats

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

    let update (stats: RpsStats) =
        async {
            let updatedStats = mapRecord stats

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
