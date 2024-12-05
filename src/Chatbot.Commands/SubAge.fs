namespace Commands

[<AutoOpen>]
module SubAge =

    open System

    let subAge args context =
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
            | None -> return Message "Missing parameters"
            | Some (user, channel) ->

                match! IVR.getSubAge user channel with
                | Error err -> return Message err
                | Ok subage ->
                    match subage.StatusHidden with
                    | None -> return Message $"Unable to look up subscription status to channel {channel}"
                    | Some true -> return Message "Subscription status hidden"
                    | Some false ->
                        match subage.Cumulative, subage.Streak with
                        | Some cum, Some streak ->
                            return Message $"%s{user} has been subscribed to %s{subage.Channel.DisplayName} for %d{cum.Months} months (%d{streak.Months} month streak)"
                        | Some cum, None ->
                            return Message $"%s{user} is not currently subscribed to %s{subage.Channel.DisplayName}. Previously subscribed for %d{cum.Months} months. Subscription ended on %s{cum.End.ToString(Utils.DateTime.DateStringFormat)}"
                        | _, _ ->
                            return Message $"%s{user} has not subscribed to %s{subage.Channel.DisplayName} before"
        }