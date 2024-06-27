namespace Chatbot.Database

module AliasRepository =

    open DB
    open Dapper.FSharp.SQLite
    open Types

    let private mapAliasEntity (entity: Entities.Alias) : Alias = {
        UserId = entity.user_id
        Name = entity.name
        Command = entity.command
    }

    let private mapAlias (record: Alias) : Entities.Alias = {
        alias_id = 0
        user_id = record.UserId
        name = record.Name
        command = record.Command
    }

    let getByUserAndName (userId: int) (alias: string) =
        async {
            let! channel =
                select {
                    for row in aliases do
                        where (row.user_id = userId && row.name = alias)
                }
                |> connection.SelectAsync<Entities.Alias>
                |> Async.AwaitTask

            return channel |> Seq.map mapAliasEntity |> Seq.tryExactlyOne
        }

    let add (alias: Alias) =
        async {
            let newAlias = mapAlias alias

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
                return DatabaseResult.Failure ex
        }

    let update (alias: Alias) =
        async {
            let updatedAlias = mapAlias alias

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
                return DatabaseResult.Failure ex
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
                return DatabaseResult.Failure ex
        }
