namespace Chatbot.Database

module DungeonRepository =

    open Chatbot
    open DB
    open Types.Dungeon

    open Dapper.FSharp.SQLite

    let private mapEntity (entity: Entities.DungeonPlayer) : DungeonPlayer = {
        UserId = entity.user_id
        Data = entity.data
    }

    let private mapRecord (record: DungeonPlayer) : Entities.DungeonPlayer = {
        user_id = record.UserId
        data = record.Data
    }

    let get (userId: int) =
        async {
            let! player =
                select {
                    for row in dungeon do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<Entities.DungeonPlayer>
                |> Async.AwaitTask

            return player |> Seq.map mapEntity |> Seq.tryExactlyOne
        }

    let add (player: DungeonPlayer) =
        async {
            let entity = mapRecord player

            try
                let! rowsAffected =
                    insert {
                        into dungeon
                        value entity
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }

    let update (player: DungeonPlayer) =
        async {
            let entity = mapRecord player

            try
                let! rowsAffected =
                    update {
                        for row in dungeon do
                            set entity
                            where (row.user_id = entity.user_id)
                    }
                    |> connection.UpdateAsync<Entities.DungeonPlayer>
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
