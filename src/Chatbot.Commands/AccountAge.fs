namespace Chatbot.Commands

[<AutoOpen>]
module AccountAge =

    open System

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Twitch
    open Chatbot.Common.Utils

    let accountAge (twitchService: TwitchService) context =
        asyncResult {
            let username =
                match context.MessageArgs with
                | [] -> context.Username
                | username :: _ -> username

            let! user =
                twitchService.Users.GetUser username
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let today = utcNow().Date
            let createdAt = user.CreatedAt.Date
            let years = today.Year - createdAt.Year
            let remainingDays = (today - createdAt.AddYears(years)).Days

            let age =
                if years > 0 then
                    $"{years}y, {remainingDays}d"
                else
                    $"{remainingDays}d"

            let creationDate = createdAt.ToString("dd MMM yyyy")

            return [ Message $"""Account created %s{age} ago on %s{creationDate}""" ]
        }
