namespace Chatbot.Database

module Reminders =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite
    open Dapper

    open Chatbot.Database.Entities
    open Chatbot.Database.Models
    open Db

    let getTimedReminders (db: Database) =
        async {
            let query =
                """
                SELECT reminder_id, timestamp, from_user_id, from_username, target_user_id, target_username, message, channel, reminder_timestamp, reminded
                FROM reminders
                WHERE reminded = FALSE
                AND reminder_timestamp < datetime('now')
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

                let! results = connection.QueryAsync<Entities.TimedReminder>(query) |> Async.AwaitTask
                connection.ExecuteAsync(update, results |> Seq.map (fun r -> {| reminderId = r.reminder_id |})) |> Async.AwaitTask |> ignore

                return
                    results
                    |> Seq.map (fun r -> {
                        FromUsername = r.from_username
                        TargetUsername = r.target_username
                        Timestamp = System.DateTime.Parse r.timestamp
                        Message = r.message
                        Channel = r.channel
                    })
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

                let! results = connection.QueryAsync<Entities.Reminder>(query, {| userId = userId |}) |> Async.AwaitTask
                connection.ExecuteAsync(update, results |> Seq.map (fun r -> {| reminderId = r.reminder_id |})) |> Async.AwaitTask |> ignore

                return
                    results
                    |> Seq.map (fun r -> {
                        FromUsername = r.from_username
                        TargetUsername = r.target_username
                        Timestamp = System.DateTime.Parse r.timestamp
                        Message = r.message
                    })
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

                return DatabaseResult.Success count
            with ex ->
                return DatabaseResult.Failure
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

                return DatabaseResult.Success count
            with ex ->
                return DatabaseResult.Failure
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

                return DatabaseResult.Success id
            with ex ->
                return DatabaseResult.Failure
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

                return DatabaseResult.Success rowsAffected
            with ex ->
                return DatabaseResult.Failure
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

                return DatabaseResult.Success rowsAffected
            with ex ->
                return DatabaseResult.Failure
        }
