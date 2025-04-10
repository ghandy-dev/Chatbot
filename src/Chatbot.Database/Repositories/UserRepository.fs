namespace Database

module UserRepository =

    open DB
    open Database.Entities
    open Database.Models

    open Dapper.FSharp.SQLite

    let get (userId: int) =
        async {
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

    let add (user: NewUser) =
        async {
            let newUser = {
                user_id = user.UserId
                username = user.Username
                is_admin = user.IsAdmin
            }

            try
                let! rowsAffected =
                    insert {
                        into users
                        value newUser
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
