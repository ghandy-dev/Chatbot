namespace Chatbot.Database

module AliasRepository =

    open Chatbot
    open DB
    open Types.Aliases

    open Dapper.FSharp.SQLite

    let private mapEntity (entity: Entities.Alias) : Alias = {
        UserId = entity.user_id
        Name = entity.name
        Command = entity.command
    }

    let private mapRecord (record: Alias) : Entities.Alias = {
        alias_id = 0
        user_id = record.UserId
        name = record.Name
        command = record.Command
    }

    let get (userId: int) (alias: string) =
        async {
            let! results =
                select {
                    for row in aliases do
                        where (row.user_id = userId && row.name = alias)
                }
                |> connection.SelectAsync<Entities.Alias>
                |> Async.AwaitTask

            return results |> Seq.map mapEntity |> Seq.tryExactlyOne
        }

    let add (alias: Alias) =
        async {
            let newAlias = mapRecord alias

            try
                let! rowsAffected =
                    insert {
                        for row in aliases do
                        value newAlias
                        excludeColumn row.alias_id
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }

    let update (alias: Alias) =
        async {
            let updatedAlias = mapRecord alias

            try
                let! rowsAffected =
                    update {
                        for row in aliases do
                        set updatedAlias
                        where (row.user_id = alias.UserId && row.name = alias.Name)
                        excludeColumn row.alias_id
                    }
                    |> connection.UpdateAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }

    let delete (userId: int) (alias: string) =
        async {
            try
                let! rowsAffected =
                    delete {
                        for row in aliases do
                            where (row.user_id = userId && row.name = alias)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
