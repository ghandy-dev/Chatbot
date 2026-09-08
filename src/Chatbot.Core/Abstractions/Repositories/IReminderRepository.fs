namespace Chatbot.Core.Abstractions.Repositories

open Chatbot.Core.Domain
open Chatbot.Core.Domain.Types

type IReminderRepository =
    abstract member GetTimedReminders: unit -> Async<TimedReminder seq>
    abstract member GetReminders: UserId -> Async<Reminder seq>
    abstract member GetPendingTimedReminderCount: UserId -> Async<Result<int, exn>>
    abstract member GetPendingReminderCount: UserId -> Async<Result<int, exn>>
    abstract member Add: NewReminder -> Async<Result<int, exn>>
    abstract member Update: UpdateReminder -> Async<Result<int, exn>>
    abstract member Delete: ReminderId -> Async<Result<int, exn>>
