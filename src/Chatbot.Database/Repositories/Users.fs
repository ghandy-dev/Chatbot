namespace Chatbot.Database.Repositories

[<RequireQualifiedAccess>]
module Users =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Chatbot.Core.Domain.Types
    open Chatbot.Database.Db
    open Chatbot.Database.DbModels

    let private toUser (dbUser: DbUser) : User =
        {
            UserId = dbUser.user_id
            Username = dbUser.username
            IsAdmin = dbUser.is_admin
        }

    let get (db: Database) (userId: int) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! user =
                select {
                    for row in users do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<DbUser>
                |> Async.AwaitTask

            return
                user
                |> Seq.tryHead
                |> Option.map toUser
        }

    let add (db: Database) (user: NewUser) =
        async {
            let newUser = {
                user_id = user.UserId
                username = user.Username
                is_admin = user.IsAdmin
            }

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    insert {
                        into users
                        value newUser
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let create db =

        {
            new IUsersRepository with
                member _.Get userId = get db userId
                member _.Add user = add db user
        }
