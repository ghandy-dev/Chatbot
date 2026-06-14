namespace Chatbot.Database.Repositories

[<RequireQualifiedAccess>]
module Reminders =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite
    open Dapper

    open Chatbot.Core.Domain.Types
    open Chatbot.Database.Db
    open Chatbot.Database.DbModels

    let private toTimedReminder (dbReminder: DbTimedReminder) =
        {
            FromUsername = dbReminder.from_username
            TargetUsername = dbReminder.target_username
            Timestamp = System.DateTimeOffset.Parse dbReminder.timestamp
            Message = dbReminder.message
            Channel = dbReminder.channel
        }

    let private toReminder (dbReminder: DbReminder) =
        {
            FromUsername = dbReminder.from_username
            TargetUsername = dbReminder.target_username
            Timestamp = System.DateTimeOffset.Parse dbReminder.timestamp
            Message = dbReminder.message
        }

    let getTimedReminders (db: Database) =
        async {
            let query =
                """
                SELECT reminder_id, timestamp, from_user_id, from_username, target_user_id, target_username, message, channel, reminder_timestamp, reminded
                FROM reminders
                WHERE reminded = FALSE
                AND datetime(reminder_timestamp) < datetime('now')
                """

            let update =
                """
                UPDATE reminders
                SET reminded = TRUE
                WHERE reminder_id = @reminderId
                """

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! results = connection.QueryAsync<DbTimedReminder>(query) |> Async.AwaitTask
                connection.ExecuteAsync(update, results |> Seq.map (fun r -> {| reminderId = r.reminder_id |})) |> Async.AwaitTask |> ignore

                return
                    results
                    |> Seq.map toTimedReminder
            with ex ->
                return []
        }

    let getReminders (db: Database) (userId: int) =
        async {
            let query =
                """
                SELECT reminder_id, timestamp, from_user_id, from_username, target_user_id, target_username, message, reminded
                FROM reminders
                WHERE target_user_id = @userId
                AND reminded = FALSE
                AND reminder_timestamp IS NULL
                """

            let update =
                """
                UPDATE reminders
                SET reminded = TRUE
                WHERE reminder_id = @reminderId
                """

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! transaction = connection.BeginTransactionAsync().AsTask() |> Async.AwaitTask

                let! results = connection.QueryAsync<DbReminder>(query, {| userId = userId |}) |> Async.AwaitTask
                let! _ = connection.ExecuteAsync(update, results |> Seq.map (fun r -> {| reminderId = r.reminder_id |})) |> Async.AwaitTask

                transaction.CommitAsync() |> Async.AwaitTask |> ignore

                return
                    results
                    |> Seq.map toReminder
            with ex ->
                return []
        }

    let getPendingTimedReminderCount (db: Database) (userId: int) =
        async {
            let query =
                """
                SELECT COUNT(*)
                FROM reminders
                WHERE target_user_id = @userId
                AND REMINDED = FALSE
                AND reminder_timestamp IS NOT NULL
                """

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! count = connection.ExecuteScalarAsync<int>(query, {| userId = userId |}) |> Async.AwaitTask

                return Ok count
            with ex ->
                return Error ex
        }

    let getPendingReminderCount (db: Database) (userId: int) =
        async {
            let query =
                """
                SELECT COUNT(*)
                FROM reminders
                WHERE target_user_id = @userId
                AND REMINDED = FALSE
                AND reminder_timestamp IS NULL
                """

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! count = connection.ExecuteScalarAsync<int>(query, {| userId = userId |}) |> Async.AwaitTask

                return Ok count
            with ex ->
                return Error ex
        }

    let add (db: Database) (reminder: NewReminder) =
        async {
            let query =
                """
                INSERT INTO reminders (timestamp, from_user_id, from_username, target_user_id, target_username, channel, message, reminder_timestamp)
                VALUES (@timestamp, @fromUserId, @fromUsername, @targetUserId, @targetUsername, @channel, @message, @reminderTimestamp)
                RETURNING reminder_id
                """

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! id =
                    connection.QuerySingleAsync<int>(
                        query,
                        {|
                            timestamp = reminder.Timestamp
                            fromUserId = reminder.FromUserId
                            fromUsername = reminder.FromUsername
                            targetUserId = reminder.TargetUserId
                            targetUsername = reminder.TargetUsername
                            channel = reminder.Channel
                            message = reminder.Message
                            reminderTimestamp = reminder.ReminderTimestamp
                        |}
                    )
                    |> Async.AwaitTask

                return Ok id
            with ex ->
                return Error ex
        }

    let update (db: Database) (reminder: UpdateReminder) =
        async {
            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    update {
                        for row in reminders do
                            setColumn row.message reminder.Message
                            where (row.reminded = 0 && row.from_user_id = reminder.UserId)
                    }
                    |> connection.UpdateAsync
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let delete (db: Database) (reminderId: int) =
        async {
            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    delete {
                        for row in reminders do
                            where (row.reminder_id = reminderId)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let create db =

        {
            new IReminderRepository with
                member _.Add newReminder = add db newReminder
                member _.Delete reminderId = delete db reminderId
                member _.GetReminders userId = getReminders db userId
                member _.GetPendingReminderCount userId = getPendingReminderCount db userId
                member _.GetPendingTimedReminderCount userId = getPendingTimedReminderCount db userId
                member _.GetTimedReminders () = getTimedReminders db
                member _.Update updateReminder = update db updateReminder
        }
