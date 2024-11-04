namespace Database

module ReminderRepository =

    open DB
    open Types.Reminders

    open Dapper.FSharp.SQLite
    open Dapper

    type private ReminderQuery = {
        reminder_id: int
        timestamp: string
        from_username: string
        target_username: string
        message: string
    }

    type private TimedReminderQuery = {
        reminder_id: int
        timestamp: string
        from_username: string
        target_username: string
        message: string
        channel: string
    }

    let private mapReminderQuery (query: ReminderQuery) : Reminder = {
        FromUsername = query.from_username
        TargetUsername = query.target_username
        Timestamp = System.DateTime.Parse query.timestamp
        Message = query.message
    }

    let private mapTimedReminderQuery (query: TimedReminderQuery) : TimedReminder = {
        FromUsername = query.from_username
        TargetUsername = query.target_username
        Timestamp = System.DateTime.Parse query.timestamp
        Message = query.message
        Channel = query.channel
    }

    let getTimedReminders () =
        async {
            let query =
                """
                SELECT reminder_id, timestamp, from_username, target_username, message, channel
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
                let! results = connection.QueryAsync<TimedReminderQuery>(query) |> Async.AwaitTask
                connection.ExecuteAsync(update, results |> Seq.map (fun r -> {| reminderId = r.reminder_id |})) |> Async.AwaitTask |> ignore
                return results |> Seq.map mapTimedReminderQuery
            with ex ->
                Logging.error "Error executing query" ex
                return []
        }

    let getReminders (userId: int) =
        async {
            let query =
                """
                SELECT reminder_id, timestamp, from_username, target_username, message
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
                let! results = connection.QueryAsync<ReminderQuery>(query, {| userId = userId |}) |> Async.AwaitTask
                connection.ExecuteAsync(update, results |> Seq.map (fun r -> {| reminderId = r.reminder_id |})) |> Async.AwaitTask |> ignore
                return results |> Seq.map mapReminderQuery
            with ex ->
                Logging.error "Error retrieving reminders" ex
                return []
        }

    let getPendingTimedReminderCount (userId: int) =
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
                let! count = connection.ExecuteScalarAsync<int>(query, {| userId = userId |}) |> Async.AwaitTask
                return DatabaseResult.Success count
            with ex ->
                Logging.error "Error executing query" ex
                return DatabaseResult.Failure
        }

    let getPendingReminderCount (userId: int) =
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
                let! count = connection.ExecuteScalarAsync<int>(query, {| userId = userId |}) |> Async.AwaitTask
                return DatabaseResult.Success count
            with ex ->
                Logging.error "Error executing query" ex
                return DatabaseResult.Failure
        }

    let add (reminder: CreateReminder) =
        async {
            let query =
                """
                INSERT INTO reminders (timestamp, from_user_id, from_username, target_user_id, target_username, channel, message, reminder_timestamp)
                VALUES (@timestamp, @fromUserId, @fromUsername, @targetUserId, @targetUsername, @channel, @message, @reminderTimestamp);

                SELECT last_insert_rowid();
                """

            try
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
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }

    let update (reminder: UpdateReminder) =
        async {
            try
                let! rowsAffected =
                    update {
                        for row in reminders do
                            setColumn row.message reminder.Message
                            where (row.reminded = 0 && row.user_id = reminder.UserId)
                    }
                    |> connection.UpdateAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }

    let delete (reminderId: int) =
        async {
            try
                let! rowsAffected =
                    delete {
                        for row in reminders do
                            where (row.reminder_id = reminderId)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
