namespace Commands

[<AutoOpen>]
module NameColor =

    open FsToolkit.ErrorHandling

    let twitchService = Services.services.TwitchService

    let namecolor (args: string list) (context: Context) =
        asyncResult {
            let username = args |> List.tryHead |? context.Username
            let! user =
                twitchService.GetUser username
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            match! twitchService.GetUserChatColor user.Id |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - UserChatColor") with
            | None -> return Message "User not found"
            | Some userColor -> return Message $"{userColor.UserName} {userColor.Color}"
        }
