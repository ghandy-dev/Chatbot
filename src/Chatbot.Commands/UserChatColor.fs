namespace Chatbot.Commands

[<AutoOpen>]
module NameColor =

    open TTVSharp.Helix

    let namecolor (args: string list) (context: Context) =
        async {
            let username =
                match args with
                | [] -> context.Username
                | username :: _ -> username

            match! Chat.getUserChatColor username |-> Result.fromOption "User not found" with
            | Error err -> return Error err
            | Ok response -> return Ok <| Message $"{response.UserName} {response.Color}"
        }
