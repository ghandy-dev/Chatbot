namespace Chatbot.Commands

[<AutoOpen>]
module UserId =

    open TTVSharp.Helix

    let userId (args: string list) (context: Context) =
        async {
            match args with
            | [] -> return Ok <| Message context.UserId
            | username :: _ ->
                match! Users.getUser username with
                | Some user -> return Ok <| Message user.Id
                | None -> return Error "User not found"
        }
