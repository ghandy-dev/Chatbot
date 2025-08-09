namespace Commands

[<AutoOpen>]
module Remind =

    open System.Text.RegularExpressions

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open CommandError
    open Database

    let private whenPattern username = sprintf @"(%s) (in|at|on|tomorrow|next\s*)" username
    let twitchService = Services.services.TwitchService

    let private setTimedReminder (user: string) (content: string) (context: Context) =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "Timed reminders can only be used in channels"
            | Channel channel ->
                let! datetime, start, ``end`` = DateTime.tryParseNaturalLanguageDateTime content |> Option.toResultWith (InvalidArgs "Couldn't parse reminder time")
                let now = utcNow()
                let reminderTimestamp = datetime.ToUniversalTime()
                if (reminderTimestamp - now).Days / 365 > 5 then
                    return! invalidArgs "Max reminder time span is now + 5 years"
                else
                    let message = content[``end`` + 1..]
                    let timespan = reminderTimestamp.AddSeconds(1) - now

                    let! targetUser =
                        twitchService.GetUser user
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                        |> AsyncResult.bindRequireSome (InvalidArgs "Target user not found")

                    let reminder = Models.NewReminder.create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName (Some channel.Channel) message (Some reminderTimestamp)
                    let targetUsername = if targetUser.Id = context.UserId then "you" else $"@%s{targetUser.DisplayName}"

                    match! ReminderRepository.add reminder with
                    | DatabaseResult.Success id -> return Message $"(ID: %d{id}) I will remind %s{targetUsername} in %s{formatTimeSpan timespan}"
                    | DatabaseResult.Failure -> return Message "Error occurred trying to create reminder"
        }

    let private setReminder (user: string) (message: string) (context: Context) =
        asyncResult {
            let! targetUser =
                twitchService.GetUser user
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "Target user not found")

            let reminder = Models.NewReminder.create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName None message None

            match! ReminderRepository.add reminder with
            | DatabaseResult.Success id -> return Message $"(ID: %d{id}) I will remind {targetUser.DisplayName} when they next type in chat"
            | DatabaseResult.Failure -> return Message "Error occurred trying to create reminder"
        }

    let private remind' (args: string seq) (user: string) (context: Context) =
        asyncResult {
            let content = String.concat " " args
            let isTimedReminder = Regex.IsMatch(sprintf $"%s{user} %s{content}", whenPattern user, RegexOptions.IgnoreCase)

            if isTimedReminder then
                match! ReminderRepository.getPendingTimedReminderCount (context.UserId |> int) with
                | DatabaseResult.Failure -> return Message "Error occured checking current pending reminders"
                | DatabaseResult.Success c when c > 20 -> return Message "User has too many pending timed reminders"
                | DatabaseResult.Success _ -> return! setTimedReminder user content context
            else
                match! ReminderRepository.getPendingReminderCount (context.UserId |> int) with
                | DatabaseResult.Failure -> return Message "Error occured checking current pending reminders"
                | DatabaseResult.Success c when c > 10 -> return Message "User has too many pending reminders"
                | DatabaseResult.Success _ -> return! setReminder user content context
        }

    let remind args context =
        asyncResult {
            match args with
            | [] -> return! invalidArgs "No user/message provided"
            | [ _ ] -> return! invalidArgs "No message provided"
            | "me" :: rest ->
                let user = context.Username
                return! remind' rest user context
            | user :: rest ->
                return! remind' rest user context
        }
