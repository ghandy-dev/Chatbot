namespace Commands

[<AutoOpen>]
module FollowAge =

    let followAge args context =
        async {
            let maybeData =
                match context.Source with
                | Whisper _ ->
                    match args with
                    | [] -> None
                    | user :: channel :: _ -> Some (user, channel)
                    | _ -> None
                | Channel channel ->
                    match args with
                    | [] -> Some (context.Username, channel.Channel)
                    | user :: channel :: _ -> Some (user, channel)
                    | user :: _ -> Some (user, channel.Channel)

            match maybeData with
            | None -> return Message "You must specify a user and channel when using this command in whispers"
            | Some (user, channel) ->
                match! IVR.getSubAge user channel with
                | Error err -> return Message err
                | Ok subage ->
                    let self = if System.String.Compare(user, context.Username, ignoreCase = true) = 0 then true else false

                    match subage.FollowedAt, self with
                    | None, false -> return Message $"%s{user} is not following %s{channel}"
                    | None, true -> return Message $"You are not following %s{channel}"
                    | Some followedAt, false ->
                        let duration = System.DateTimeOffset.UtcNow - followedAt |> formatTimeSpan
                        return Message $"%s{user} has been following %s{channel} for %s{duration}"
                    | Some followedAt, true ->
                        let duration = System.DateTimeOffset.UtcNow - followedAt |> formatTimeSpan
                        return Message $"You have been following %s{channel} for %s{duration}"
        }