namespace Chatbot.Database.Services

module ReminderService =

    open Chatbot.Database
    open Chatbot.Database.Types

    let create db =

        {
            new IReminderService with
                member _.Add newReminder = Reminders.add db newReminder
                member _.Delete reminderId = Reminders.delete db reminderId
                member _.GetReminders userId = Reminders.getReminders db userId
                member _.GetPendingRemindersCount userId = Reminders.getPendingReminderCount db userId
                member _.GetPendingTimedReminderCount userId = Reminders.getPendingTimedReminderCount db userId
                member _.GetTimedReminders () = Reminders.getTimedReminders db
                member _.Update updateReminder = Reminders.update db updateReminder
        }
