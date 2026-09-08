namespace Chatbot.Core.Domain

open Types

type Reminder =
    {
        FromUsername: string
        TargetUsername: string
        Timestamp: System.DateTimeOffset
        Message: string
    }

and TimedReminder =
    {
        FromUsername: string
        TargetUsername: string
        Timestamp: System.DateTimeOffset
        Message: string
        Channel: string
    }

and NewReminder =
    {
        FromUserId: UserId
        FromUsername: string
        TargetUserId: UserId
        TargetUsername: string
        Channel: string option
        Message: string
        Timestamp: System.DateTimeOffset
        ReminderTimestamp: System.DateTimeOffset option
    }

    static member create fromUserId fromUsername targetUserId targetUsername channel message reminderTimestamp =
        {
            FromUserId = fromUserId
            FromUsername = fromUsername
            TargetUserId = targetUserId
            TargetUsername = targetUsername
            Channel = channel
            Message = message
            Timestamp = System.DateTimeOffset.UtcNow
            ReminderTimestamp = reminderTimestamp
        }

and UpdateReminder =
    {
        ReminderId: int
        UserId: UserId
        Message: string
    }

    static member create reminderId userId targetUserId message =
        {
            ReminderId = reminderId
            UserId = userId
            Message = message
        }
