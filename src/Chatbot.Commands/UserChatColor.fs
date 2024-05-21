namespace Chatbot.Commands

[<AutoOpen>]
module NameColor =

    open Chatbot
    open Chatbot.HelixApi
    open TTVSharp.Helix

    let private userChatColor (user: User) =
        async {
            return!
                helixApi.Chat.GetUserChatColorAsync(new GetUserChatColorRequest(UserIds = [ user.Id ])) |> Async.AwaitTask
                |+-> TTVSharp.tryHeadResultT "User color not found. Waduheck."
        }

    let private innerNameColor username =
        async {
            match! Users.getUser username |+-> TTVSharp.tryHeadResult "User not found." |++> Result.bindAsync userChatColor with
            | Ok response -> return Ok $"{response.UserName} {response.Color}"
            | Error err -> return Error err
        }

    let namecolor (args: string list) (context: Context) =
        async {
            let username =
                match args with
                | [] -> context.Username
                | username :: _ -> username

            match! innerNameColor username with
            | Error err -> return Error err
            | Ok message -> return Ok <| Message message
        }
