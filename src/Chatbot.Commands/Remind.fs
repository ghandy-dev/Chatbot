namespace Chatbot.Commands

[<AutoOpen>]
module Remind =

    open System.Text.RegularExpressions

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain
    open Chatbot.Core.Domain.Types
    open Chatbot.Core.Services.Twitch

    let private whenPattern username = sprintf @"^(%s) (in|at|on|tomorrow|next\s*)" username

    let private setTimedReminder (reminders: IReminderRepository) (twitchService: TwitchService) (user: string) (content: string) (context: Context) =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "Timed reminders can only be used in channels"
            | Channel (channel, _) ->
                let! datetime, _, ``end`` = DateTime.tryParseNaturalLanguageDateTime content |> Option.toResultWith (InvalidArgs "Couldn't parse reminder time")
                let now = utcNow()
                let maxDate = now.AddYears(5)
                let reminderTimestamp = datetime

                if reminderTimestamp > maxDate then
                    return! invalidArgs "Max reminder time span is now + 5 years"
                else
                    let message = content[``end`` + 1..]

                    let! targetUser =
                        twitchService.Users.GetUser user
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                        |> AsyncResult.bindRequireSome (InvalidArgs "Target user not found")

                    let newReminder = NewReminder.create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName (Some channel) message (Some reminderTimestamp)
                    let targetUsername = if targetUser.Id = context.UserId then "you" else $"@%s{targetUser.DisplayName}"

                    return!
                        async {
                            match! reminders.Add newReminder with
                            | Error _ -> return [ Message "Error occurred trying to create reminder" ]
                            | Ok id -> return [ Message $"(ID: %d{id}) I will remind %s{targetUsername} in %s{formatElapsed now reminderTimestamp}" ]
                        }
        }

    let private setReminder (reminders: IReminderRepository) (twitchService: TwitchService) (user: string) (message: string) (context: Context) =
        asyncResult {
            let! targetUser =
                twitchService.Users.GetUser user
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "Target user not found")

            let newReminder = NewReminder.create (context.UserId |> int) context.Username (targetUser.Id |> int) targetUser.DisplayName None message None

            return!
                async {
                    match! reminders.Add newReminder with
                    | Error _ -> return internalError "Error occurred trying to create reminder"
                    | Ok id -> return Ok [ Message $"(ID: %d{id}) I will remind {targetUser.DisplayName} when they next type in chat" ]
                }
        }

    let private remind' (reminders: IReminderRepository) twitchService (args: string seq) (user: string) (context: Context) =
        async {
            let content = String.join " " args
            let isTimedReminder = Regex.IsMatch(sprintf $"%s{user} %s{content}", whenPattern user, RegexOptions.IgnoreCase)

            if isTimedReminder then
                match! reminders.GetPendingTimedReminderCount (context.UserId |> int) with
                | Error _ -> return internalError "Error occured checking current pending reminders"
                | Ok c when c > 20 -> return Ok [ Message "User has too many pending timed reminders" ]
                | Ok _ -> return! setTimedReminder reminders twitchService user content context
            else
                match! reminders.GetPendingReminderCount (context.UserId |> int) with
                | Error _ -> return internalError "Error occured checking current pending reminders"
                | Ok c when c > 10 -> return Ok [ Message "User has too many pending reminders" ]
                | Ok _ -> return! setReminder reminders twitchService user content context
        }

    let remind (reminders: IReminderRepository) (twitchService: TwitchService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No user/message provided"
            | [ _ ] -> return! invalidArgs "No message provided"
            | "me" :: rest ->
                let user = context.Username
                return! remind' reminders twitchService rest user context
            | user :: rest ->
                return! remind' reminders twitchService rest user context
        }
