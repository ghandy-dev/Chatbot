namespace Chatbot.Database

module Aliases =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Chatbot.Database.Models
    open Chatbot.Database.Entities
    open Db

    type AliasQuery =
        | ByUserIdAliasName of userId: int * alias: string

    let get (db: Database) (query: AliasQuery) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

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

    let add (db: Database) (alias: NewAlias) =
        async {
            let newAlias = {
                alias_id = 0
                user_id = alias.UserId
                name = alias.Name
                command = alias.Command
            }

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

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
                return DatabaseResult.Failure
        }

    let update (db: Database) (alias: UpdateAlias) =
        async {
            let updatedAlias = {
                alias_id = 0
                user_id = alias.UserId
                name = alias.Name
                command = alias.Command
            }

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

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
                return DatabaseResult.Failure
        }

    let delete (db: Database) (alias: DeleteAlias) =
        async {
            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    delete {
                        for row in aliases do
                            where (row.user_id = alias.UserId && row.name = alias.Name)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                return DatabaseResult.Failure
        }
