namespace Commands

[<AutoOpen>]
module UserId =

    let twitchService = Services.services.TwitchService

    let userId (args: string list) (context: Context) =
        async {
            match args with
            | [] -> return Message context.UserId
            | username :: _ ->
                match! twitchService.GetUser username with
                | Some user -> return Message user.Id
                | None -> return Message "User not found"
        }
