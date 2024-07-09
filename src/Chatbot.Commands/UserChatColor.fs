namespace Chatbot.Commands

[<AutoOpen>]
module NameColor =

    open Chatbot
    open Chatbot.HelixApi
    open TTVSharp.Helix

    let private getNameColor username =
        async {
            return!
                Users.getUser username |+-> TTVSharp.tryHeadResult "User not found"
                |++> Result.bindAsync (fun user ->
                    helixApi.Chat.GetUserChatColorAsync(new GetUserChatColorRequest(UserIds = [ user.Id ])) |> Async.AwaitTask
                    |+-> TTVSharp.tryHeadResult "User color not found"
                )
        }

    let namecolor (args: string list) (context: Context) =
        async {
            let username =
                match args with
                | [] -> context.Username
                | username :: _ -> username

            match! getNameColor username with
            | Error err -> return Error err
            | Ok response -> return Ok <| Message $"{response.UserName} {response.Color}"
        }
