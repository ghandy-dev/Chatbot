namespace Database.Models

type Reminder = {
    FromUsername: string
    TargetUsername: string
    Timestamp: System.DateTime
    Message: string
}

and TimedReminder = {
    FromUsername: string
    TargetUsername: string
    Timestamp: System.DateTime
    Message: string
    Channel: string
}

and NewReminder = {
    FromUserId: int
    FromUsername: string
    TargetUserId: int
    TargetUsername: string
    Channel: string option
    Message: string
    Timestamp: System.DateTime
    ReminderTimestamp: System.DateTime option
} with

    static member create fromUserId fromUsername targetUserId targetUsername channel message reminderTimestamp = {
        FromUserId = fromUserId
        FromUsername = fromUsername
        TargetUserId = targetUserId
        TargetUsername = targetUsername
        Channel = channel
        Message = message
        Timestamp = System.DateTime.UtcNow
        ReminderTimestamp = reminderTimestamp
    }

and UpdateReminder = {
    ReminderId: int
    UserId: int
    Message: string
} with

    static member create reminderId userId targetUserId message = {
        ReminderId = reminderId
        UserId = userId
        Message = message
    }