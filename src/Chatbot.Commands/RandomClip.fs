namespace Commands

[<AutoOpen>]
module RandomClip =

    open System

    let twitchService = Services.services.TwitchService

    let private getChannel args (context: Context) =
        match context.Source with
        | Whisper _ ->
            match args with
            | channel :: _ -> Ok channel
            | _ -> Error "You must specify a channel when using this command in whispers"
        | Channel channel ->
            match args with
            | [] -> Ok channel.Channel
            | channel :: _ -> Ok channel

    let private keys = [ "period" ]

    let private periodToDateRange period =
        let rangeTo = DateTime.Today

        let rangeFrom =
            match period with
            | "day" -> rangeTo.AddDays(-1)
            | "week" -> rangeTo.AddDays(-7)
            | "month" -> rangeTo.AddMonths(-1)
            | "year" -> rangeTo.AddYears(-1)
            | "all"
            | _ -> DateTime.MinValue

        (rangeFrom, rangeTo)

    let randomClip (args: string list) (context: Context) =
        async {
            let values = KeyValueParser.parse args keys

            let period = values.TryFind "period" |?? "week"

            let dateFrom, dateTo = periodToDateRange period

            match!
                getChannel args context
                |> Async.create
                |> Result.bindAsync (fun username -> twitchService.GetUser username |-> Result.fromOption "User not found")
                |> Result.bindAsync (fun user -> twitchService.GetClips user.Id dateFrom dateTo |> Result.fromOptionAsync "Twitch API error")
            with
            | Error err -> return Message err
            | Ok clips ->
                match clips |> List.ofSeq with
                | [] -> return Message "No clips found"
                | clips ->
                    let clip = clips |> List.randomChoice

                    return Message $""""{clip.Title}" - clipped on {clip.CreatedAt.ToString(DateStringFormat)}, {clip.Duration} secs, {clip.ViewCount.ToString("N0")} views - {clip.Url}"""
        }
