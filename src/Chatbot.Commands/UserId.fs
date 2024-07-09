namespace Chatbot.Commands

[<AutoOpen>]
module UserId =

    open Chatbot
    open Chatbot.HelixApi

    let userId (args: string list) (context: Context) =
        async {
            match args with
            | [] -> return Ok <| Message context.UserId
            | user :: _ ->
                match! Users.getUser user |+-> TTVSharp.tryHead with
                | Some user -> return Ok <| Message user.Id
                | None -> return Error "User not found"
        }
