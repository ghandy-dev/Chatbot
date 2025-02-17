namespace Commands

[<AutoOpen>]
module Remind =

    open System
    open System.Text.RegularExpressions

    open Database
    open Database.Types.Reminders

    let private whenPattern username = sprintf @"(%s) (in|at\s*)" username

    let private setTimedReminder (user: string) (content: string) (context: Context) =
        async {
            match context.Source with
            | Whisper _ -> return Message "Timed reminders can only be used in channels"
            | Channel channel ->
                let now = now()

                match DateTime.tryParseNaturalLanguageDateTime content with
                | None -> return Message "Couldn't parse reminder time"
                | Some (datetime, start, ``end``) ->
                    if (datetime - now).Days / 365 > 5 then
                        return Message "Max reminder time span is now + 5 years"
                    else
                        let message = content[``end``..]
                        let timespan = datetime - now

                        match! Twitch.Helix.Users.getUser user with
                        | None -> return Message $"Couldn't find user %s{user}"
                        | Some targetUser ->
                            let reminder = CreateReminder.Create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName (Some channel.Channel) message (Some datetime)
                            let targetUsername = if targetUser.Id = context.UserId then "you" else $"@%s{targetUser.DisplayName}"

                            match! ReminderRepository.add reminder with
                            | DatabaseResult.Success id -> return Message $"(ID: %d{id}) I will remind %s{targetUsername} in %s{formatTimeSpan timespan}"
                            | DatabaseResult.Failure -> return Message "Error occurred trying to create reminder"
        }

    let private setReminder (user: string) (message: string) (context: Context) =
        async {
            match! Twitch.Helix.Users.getUser user with
            | None -> return Message $"Couldn't find user, %s{user}"
            | Some targetUser ->
                let reminder = CreateReminder.Create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName None message None

                match! ReminderRepository.add reminder with
                | DatabaseResult.Success id -> return Message $"(ID: %d{id}) I will remind {targetUser.DisplayName} when they next type in chat"
                | DatabaseResult.Failure -> return Message "Error occurred trying to create reminder"
        }

    let private remind' (args: string seq) (user: string) (context: Context) =
        async {
            let content = String.concat " " args

            if Regex.IsMatch(sprintf $"%s{user} %s{content}", whenPattern user, RegexOptions.IgnoreCase) then
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
        async {
            match args with
            | [] -> return Message "No user/message provided"
            | [ _ ] -> return Message "No message provided"
            | "me" :: rest ->
                let user = context.Username
                return! remind' rest user context
            | user :: rest ->
                return! remind' rest user context
        }
