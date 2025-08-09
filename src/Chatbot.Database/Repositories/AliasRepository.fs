namespace Database

module AliasRepository =

    open Dapper.FSharp.SQLite

    open Database.Models
    open Database.Entities
    open DB

    type AliasQuery =
        | ByUserIdAliasName of userId: int * alias: string

    let get (query) =
        async {
            let! results =
                match query with
                | ByUserIdAliasName (userId, alias) ->
                    select {
                        for row in aliases do
                            where (row.user_id = userId && row.name = alias)
                    }
                |> connection.SelectAsync<Entities.Alias>
                |> Async.AwaitTask

            return
                results
                |> Seq.map (fun r -> { Command = r.command ; Name = r.name })
                |> Seq.tryExactlyOne
        }

    let add (alias: NewAlias) =
        async {
            let newAlias = {
                alias_id = 0
                user_id = alias.UserId
                name = alias.Name
                command = alias.Command
            }

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

    let update (alias: UpdateAlias) =
        async {
            let updatedAlias = {
                alias_id = 0
                user_id = alias.UserId
                name = alias.Name
                command = alias.Command
            }

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

    let delete (alias: DeleteAlias) =
        async {
            try
                let! rowsAffected =
                    delete {
                        for row in aliases do
                            where (row.user_id = alias.UserId && row.name = alias.Name)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
