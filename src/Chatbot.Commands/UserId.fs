namespace Commands

[<AutoOpen>]
module UserId =

    open Twitch.Helix

    let userId (args: string list) (context: Context) =
        async {
            match args with
            | [] -> return Message context.UserId
            | username :: _ ->
                match! Users.getUser username with
                | Some user -> return Message user.Id
                | None -> return Message "User not found"
        }
