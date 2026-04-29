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

            let age = formatTimeSpan (DateTimeOffset.UtcNow - user.CreatedAt)
            let creationDate = user.CreatedAt.ToString("dd MMM yyyy")

            return [ Message $"""Account created %s{age} ago on %s{creationDate}""" ]
        }
