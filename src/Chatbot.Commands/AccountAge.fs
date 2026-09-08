namespace Chatbot.Commands

[<AutoOpen>]
module AccountAge =

    open FsToolkit.ErrorHandling

    open Chatbot.Common.Utils
    open Chatbot.Core.Domain.Commands
    open Chatbot.Infrastructure.Services

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
            let age = formatElapsed createdAt today
            let creationDate = createdAt.ToString("dd MMM yyyy")

            return [ Message $"""Account created %s{age} ago on %s{creationDate}""" ]
        }
