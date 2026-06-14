module Chatbot.Agents.Reminder

open System

open Microsoft.Extensions.Logging

open Chatbot.Common
open Chatbot.Core.Domain
open Chatbot.Core.Domain.Types
open Chatbot.Core.IRC
open Chatbot.Core.Services.Pastebin
open Chatbot.Core.Types

type ReminderMessage =
    | TwitchEvent of TwitchEvent
    | CheckReminders

let create env (reminders: IReminderRepository) (textStorageService: ITextStorageService) (twitchChatClient: Chatbot.Twitch.TwitchClient) cancellationToken =
    new MailboxProcessor<ReminderMessage>(
        (fun mb ->
            let logger = env.Logger

            let checkReminders () =
                async {
                    let! reminders = reminders.GetTimedReminders ()

                    for reminder in reminders do
                        let now = utcNow()
                        let ts = reminder.Timestamp
                        let sender = if reminder.FromUsername = reminder.TargetUsername then "yourself" else $"@%s{reminder.FromUsername}"
                        let message = $"@%s{reminder.TargetUsername}, reminder from %s{sender} (%s{formatElapsed ts now} ago): %s{reminder.Message}"
                        do twitchChatClient.Send(Request.privMsg reminder.Channel message)

                    do! Async.Sleep(250)
                    mb.Post CheckReminders
                }

            let userMessaged channel userId username =
                async {
                    match! reminders.GetPendingReminderCount userId with
                    | Ok c when c > 0 ->
                        let! reminders = reminders.GetReminders userId

                        let message =
                            reminders
                            |> Seq.groupBy (fun r -> r.FromUsername)
                            |> Seq.map (fun (_, rs) ->
                                let sender = rs |> Seq.head |> fun r -> if r.FromUsername = r.TargetUsername then "yourself" else $"@%s{r.FromUsername}"

                                let message =
                                    rs
                                    |> Seq.map (fun r ->
                                        let now = utcNow()
                                        let ts = r.Timestamp
                                        $"(%s{formatElapsed ts now} ago): %s{r.Message}"
                                    )
                                    |> String.join ", "

                                if rs |> Seq.length = 1 then
                                    $"reminder from %s{sender} %s{message}"
                                else
                                    $"reminders from %s{sender} %s{message}"
                            )
                            |> String.join ", "

                        if message.Length > 500 then
                            match! textStorageService.CreatePost "" message with
                            | Error err -> logger.LogError("Failed to create paste: {err}", err)
                            | Ok url -> do twitchChatClient.Send(Request.privMsg channel $"@%s{username}, reminders were too long to send, check %s{url} for your reminders")
                        else
                            do twitchChatClient.Send(Request.privMsg channel $"@%s{username}, %s{message}")
                    | _ -> ()
                }

            let rec loop () =
                async {
                    match! mb.Receive() with
                    | CheckReminders -> do! checkReminders ()
                    | TwitchEvent (ChannelMessage message) -> do! userMessaged message.Channel (int message.UserId) message.Username
                    | TwitchEvent (ChannelReplyMessage message) -> do! userMessaged message.Channel (int message.UserId) message.Username
                    | _ -> ()

                    return! loop ()
                }

            logger.LogInformation "Reminder agent started."
            mb.Post CheckReminders
            loop ()
        ), cancellationToken
    )
