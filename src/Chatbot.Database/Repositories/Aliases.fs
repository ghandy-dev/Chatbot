namespace Chatbot.Database.Repositories

[<RequireQualifiedAccess>]
module Aliases =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Chatbot.Core.Domain.Types
    open Chatbot.Database.Db
    open Chatbot.Database.DbModels

    let private toAlias (dbAlias: DbAlias) : Alias =
        {
            Name = dbAlias.name
            Command = dbAlias.command
        }

    let get (db: Database) (userId: int) (alias: string) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! results =
                select {
                    for row in aliases do
                        where (row.user_id = userId && row.name = alias)
                }
                |> connection.SelectAsync<DbAlias>
                |> Async.AwaitTask

            return
                results
                |> Seq.tryHead
                |> Option.map toAlias
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

                return Ok rowsAffected
            with ex ->
                return Error ex
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

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let delete (db: Database) aliasName userId =
        async {
            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    delete {
                        for row in aliases do
                            where (row.user_id = userId && row.name = aliasName)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let create db =

        {
            new IAliasRepository with
                member _.Add alias = add db alias
                member _.Delete aliasName userId = delete db aliasName userId
                member _.Get userId aliasName = get db userId aliasName
                member _.Update alias = update db alias
        }
