namespace Chatbot.Commands

[<AutoOpen>]
module NameColor =

    open TTVSharp.Helix

    let namecolor (args: string list) (context: Context) =
        async {
            let username =
                args |> List.tryHead |> Option.bind Some |> Option.defaultValue context.Username

            match! Chat.getUserChatColor username |-> Result.fromOption "User not found" with
            | Error err -> return Message err
            | Ok response -> return Message $"{response.UserName} {response.Color}"
        }
