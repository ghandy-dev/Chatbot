namespace Chatbot.Commands

[<AutoOpen>]
module Remind =

    open System.Text.RegularExpressions

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Database
    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain
    open Chatbot.Core.Services.Twitch

    let private whenPattern username = sprintf @"^(%s) (in|at|on|tomorrow|next\s*)" username

    let private setTimedReminder db (twitchService: TwitchService) (user: string) (content: string) (context: Context) =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "Timed reminders can only be used in channels"
            | Channel (channel, _) ->
                let! datetime, start, ``end`` = DateTime.tryParseNaturalLanguageDateTime content |> Option.toResultWith (InvalidArgs "Couldn't parse reminder time")
                let now = utcNow()
                let reminderTimestamp = datetime.ToUniversalTime()
                if (reminderTimestamp - now).Days / 365 > 5 then
                    return! invalidArgs "Max reminder time span is now + 5 years"
                else
                    let message = content[``end`` + 1..]
                    let timespan = reminderTimestamp.AddSeconds(1) - now

                    let! targetUser =
                        twitchService.Users.GetUser user
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                        |> AsyncResult.bindRequireSome (InvalidArgs "Target user not found")

                    let reminder = Models.NewReminder.create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName (Some channel) message (Some reminderTimestamp)
                    let targetUsername = if targetUser.Id = context.UserId then "you" else $"@%s{targetUser.DisplayName}"

                    match! Reminders.add db reminder with
                    | DatabaseResult.Success id -> return [ Message $"(ID: %d{id}) I will remind %s{targetUsername} in %s{formatTimeSpan timespan}" ]
                    | DatabaseResult.Failure -> return [ Message "Error occurred trying to create reminder" ]
        }

    let private setReminder db (twitchService: TwitchService) (user: string) (message: string) (context: Context) =
        asyncResult {
            let! targetUser =
                twitchService.Users.GetUser user
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "Target user not found")

            let reminder = Models.NewReminder.create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName None message None

            match! Reminders.add db reminder with
            | DatabaseResult.Success id -> return [ Message $"(ID: %d{id}) I will remind {targetUser.DisplayName} when they next type in chat" ]
            | DatabaseResult.Failure -> return [ Message "Error occurred trying to create reminder" ]
        }

    let private remind' db twitchService (args: string seq) (user: string) (context: Context) =
        asyncResult {
            let content = String.concat " " args
            let isTimedReminder = Regex.IsMatch(sprintf $"%s{user} %s{content}", whenPattern user, RegexOptions.IgnoreCase)

            if isTimedReminder then
                match! Reminders.getPendingTimedReminderCount db (context.UserId |> int) with
                | DatabaseResult.Failure -> return [ Message "Error occured checking current pending reminders" ]
                | DatabaseResult.Success c when c > 20 -> return [ Message "User has too many pending timed reminders" ]
                | DatabaseResult.Success _ -> return! setTimedReminder db twitchService user content context
            else
                match! Reminders.getPendingReminderCount db (context.UserId |> int) with
                | DatabaseResult.Failure -> return [ Message "Error occured checking current pending reminders" ]
                | DatabaseResult.Success c when c > 10 -> return [ Message "User has too many pending reminders" ]
                | DatabaseResult.Success _ -> return! setReminder db twitchService user content context
        }

    let remind db (twitchService: TwitchService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No user/message provided"
            | [ _ ] -> return! invalidArgs "No message provided"
            | "me" :: rest ->
                let user = context.Username
                return! remind' db twitchService rest user context
            | user :: rest ->
                return! remind' db twitchService rest user context
        }
