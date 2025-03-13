namespace Commands

[<AutoOpen>]
module AccountAge =

    open System

    let twitchService = Services.services.TwitchService

    let accountAge args context =
        async {
            let username =
                match args with
                | [] -> context.Username
                | username :: _ -> username

            match! twitchService.GetUser username with
            | None -> return Message "User not found"
            | Some user ->
                let age = formatTimeSpan (DateTimeOffset.UtcNow - user.CreatedAt)
                return Message $"""Account created %s{age} ago on %s{user.CreatedAt.ToString("dd MMM yyyy")}"""
        }
