namespace Chatbot.Database

module UserRepository =

    open Chatbot

    open DB

    open Dapper.FSharp.SQLite

    open Types


    let private mapUserEntity (entity: Entities.User) : User = {
        UserId = entity.user_id
        Username = entity.username
        IsAdmin = entity.is_admin
    }

    let private mapUser (record: User) : Entities.User = {
        user_id = record.UserId
        username = record.Username
        is_admin = record.IsAdmin
    }

    let getById (userId: int) =
        async {
            let! user =
                select {
                    for row in users do
                        where (row.user_id = userId)
                }
                |> connection.SelectAsync<Entities.User>
                |> Async.AwaitTask

            return user |> Seq.map mapUserEntity |> Seq.tryExactlyOne
        }

    let add (user: User) =
        async {
            let newUser = mapUser user

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
