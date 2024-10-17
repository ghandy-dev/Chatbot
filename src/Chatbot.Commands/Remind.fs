namespace Commands

[<AutoOpen>]
module Remind =

    open System
    open System.Text.RegularExpressions

    open Database
    open Database.Types.Reminders

    let private whenPattern = @"(in\s*)+((\d+)\s+(years?|months?|days?|hours?|minutes?|mins?|seconds?|secs?)+?,*\s*)+"
    let private timePattern = @"(\d+)\s+(years?|months?|days?|hours?|minutes?|mins?|seconds?|secs?)"

    let private parseTimeComponents (durations: (string * string) seq) =
        let parseTimeComponent (dateTime: DateTime) (duration: string) (timeComp: string) =
            let duration = Int32.Parse duration
            match duration, timeComp.ToLower() with
            | d, "year"
            | d, "years" -> dateTime.AddYears(d)
            | d, "month"
            | d, "months" -> dateTime.AddMonths(d)
            | d, "day"
            | d, "days" -> dateTime.AddDays(d)
            | d, "hour"
            | d, "hours" -> dateTime.AddHours(d)
            | d, "minute"
            | d, "minutes"
            | d, "min"
            | d, "mins" -> dateTime.AddMinutes(d)
            | d, "second"
            | d, "seconds"
            | d, "sec"
            | d, "secs" -> dateTime.AddSeconds(d)
            | _ -> dateTime

        durations |> Seq.fold (fun dt (d, tc) -> parseTimeComponent dt d tc) (utcNow())

    let private parseTimedReminder user content context =
        async {
            match context.Source with
            | Whisper _ -> return Message "Timed reminders can only be used in channels"
            | Channel channel ->
                let now = utcNow()
                let whenInput = Regex.Matches(content, whenPattern).[0].Value
                let timeComponents = Regex.Matches(whenInput, timePattern)
                let remindDateTime =
                    timeComponents
                    |> Seq.map (fun m -> m.Groups[1].Value, m.Groups[2].Value)
                    |> parseTimeComponents

                if (remindDateTime - now).Days / 365 > 5 then
                    return Message "Max reminder time span is now +5 years"
                else
                    let message = Regex.Replace(content, whenPattern, "")
                    let remindIn = remindDateTime - now

                    match! Twitch.Helix.Users.getUser user with
                    | None -> return Message $"{user} doesn't exist"
                    | Some targetUser ->
                        let reminder = CreateReminder.Create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName (Some channel.Channel) message (Some remindDateTime)
                        let targetUsername = if targetUser.Id = context.UserId then "you" else $"@{targetUser.DisplayName}"
                        match! ReminderRepository.add reminder with
                        | DatabaseResult.Success id -> return Message $"(ID: {id}) I will remind {targetUsername} in {formatTimeSpan remindIn}"
                        | DatabaseResult.Failure -> return Message "Error occurred trying to create reminder"
        }

    let private parseReminder user message (context: Context) =
        async {
            match! Twitch.Helix.Users.getUser user with
            | None -> return Message $"{user} doesn't exist or is currently banned"
            | Some targetUser ->
                let reminder = CreateReminder.Create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName None message None

                match! ReminderRepository.add reminder with
                | DatabaseResult.Success id -> return Message $"(ID: {id}) I will remind {targetUser.DisplayName} when they next type in chat"
                | DatabaseResult.Failure -> return Message "Error occurred trying to create reminder"
        }

    let private remind' args user (context: Context) =
        async {
                let content = String.concat " " args
                if Regex.IsMatch(content, whenPattern, RegexOptions.IgnoreCase) then
                    match! ReminderRepository.getPendingTimedReminderCount (context.UserId |> int) with
                    | -1 -> return Message "Error occured checking current pending reminders"
                    | c when c > 20 -> return Message "User has too many pending timed reminders"
                    | _ -> return! parseTimedReminder user content context
                else
                    match! ReminderRepository.getPendingReminderCount (context.UserId |> int) with
                    | -1 -> return Message "Error occured checking current pending reminders"
                    | c when c > 10 -> return Message "User has too many pending reminders"
                    | _ -> return! parseReminder user content context
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
