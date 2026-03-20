namespace Database

module Users =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Database.Entities
    open Database.Models
    open DB

    let get (db: Database) (userId: int) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! user =
                select {
                    for row in users do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<Entities.User>
                |> Async.AwaitTask

            return
                user
                |> Seq.map (fun r -> {
                    UserId = r.user_id
                    Username = r.username
                    IsAdmin = r.is_admin
                } : Entities.User -> Models.User)
                |> Seq.tryHead
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

                return DatabaseResult.Success rowsAffected
            with ex ->
                return DatabaseResult.Failure
        }
