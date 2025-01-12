namespace Commands

[<AutoOpen>]
module SubAge =

    let subAge args context =
        async {
            let maybeData: (string * string) option =
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
                    match subage.StatusHidden with
                    | None -> return Message $"Unable to look up subscription status to channel {channel}"
                    | Some true -> return Message "Subscription status hidden"
                    | Some false ->
                        let self = if System.String.Compare(user, context.Username, ignoreCase = true) = 0 then true else false

                        match subage.Cumulative, subage.Streak, self with
                        | Some stats, Some streak, true ->
                            return Message $"You have been subscribed to %s{subage.Channel.DisplayName} for %d{stats.Months} month(s) (%d{streak.Months} month streak)"
                        | Some stats, Some streak, false ->
                            return Message $"%s{user} has been subscribed to %s{subage.Channel.DisplayName} for %d{stats.Months} month(s) (%d{streak.Months} month streak)"
                        | Some stats, None, false ->
                            return Message $"%s{user} is not currently subscribed to %s{subage.Channel.DisplayName}. Previously subscribed for %d{stats.Months} month(s). Subscription ended on %s{stats.End.ToString(Utils.DateTime.DateStringFormat)}"
                        | Some stats, None, true ->
                            return Message $"You are not currently subscribed to %s{subage.Channel.DisplayName}. Previously subscribed for %d{stats.Months} month(s). Subscription ended on %s{stats.End.ToString(Utils.DateTime.DateStringFormat)}"
                        | _, _, false ->
                            return Message $"%s{user} has not subscribed to %s{subage.Channel.DisplayName} before"
                        | _, _, true ->
                            return Message $"You have not subscribed to %s{subage.Channel.DisplayName} before"
        }