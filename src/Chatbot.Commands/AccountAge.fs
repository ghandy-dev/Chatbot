namespace Commands

[<AutoOpen>]
module AccountAge =

    open System

    open FsToolkit.ErrorHandling

    let twitchService = Services.services.TwitchService

    let accountAge context =
        asyncResult {
            let username =
                match context.Args with
                | [] -> context.Username
                | username :: _ -> username

            let! user =
                twitchService.GetUser username
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let age = formatTimeSpan (DateTimeOffset.UtcNow - user.CreatedAt)
            let creationDate = user.CreatedAt.ToString("dd MMM yyyy")
            return Message $"""Account created %s{age} ago on %s{creationDate}"""
        }
