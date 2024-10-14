namespace Commands

[<AutoOpen>]
module NameColor =

    open Twitch.Helix

    let namecolor (args: string list) (context: Context) =
        async {
            let username =
                args |> List.tryHead |> Option.bind Some |?? context.Username

            match!
                Users.getUser username
                |-> Result.fromOption "User not found"
                |> Result.bindAsync (fun user -> Chat.getUserChatColor user.Id |-> Result.fromOption "User not found")
            with
            | Error err -> return Message err
            | Ok response -> return Message $"{response.UserName} {response.Color}"
        }
