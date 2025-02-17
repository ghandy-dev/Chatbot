namespace Commands

[<AutoOpen>]
module Vod =

    open Twitch.Helix

    let vod args =
        async {
            match args with
            | [] -> return Message "No channel specified"
            | channel :: _ ->
                match!
                    Users.getUser channel
                    |> Result.fromOptionAsync "User not found"
                    |> Result.bindAsync (fun user -> Videos.getLatestVod user.Id |-> Result.fromOption "No VOD found")
                with
                | Error err -> return Message err
                | Ok video -> return Message $""""{video.Title}" {video.CreatedAt.ToString(Utils.DateStringFormat)} {video.Url} [{video.Duration}]"""
        }
