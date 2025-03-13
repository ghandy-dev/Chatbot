namespace Commands

[<AutoOpen>]
module NameColor =

    let twitchService = Services.services.TwitchService

    let namecolor (args: string list) (context: Context) =
        async {
            let username =
                args |> List.tryHead |> Option.bind Some |?? context.Username

            match!
                twitchService.GetUser username
                |-> Result.fromOption "User not found"
                |> Result.bindAsync (fun user -> twitchService.GetUserChatColor user.Id |-> Result.fromOption "User not found")
            with
            | Error err -> return Message err
            | Ok response -> return Message $"{response.UserName} {response.Color}"
        }
