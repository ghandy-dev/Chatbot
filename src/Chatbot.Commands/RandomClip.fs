namespace Commands

[<AutoOpen>]
module RandomClip =

    open System

    open FsToolkit.ErrorHandling

    open CommandError

    let twitchService = Services.services.TwitchService

    let private getChannel (context: Context) =
        match context.Source with
        | Whisper _ ->
            match context.Args with
            | channel :: _ -> Ok channel
            | _ -> invalidArgs "You must specify a channel when using this command in whispers"
        | Channel channel ->
            match context.Args with
            | [] -> Ok channel.Channel
            | channel :: _ -> Ok channel

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

    let private keys = [ "period" ]

    let randomClip context  =
        asyncResult {
            let kvp = KeyValueParser.parse context.Args keys
            let period = kvp.KeyValues.TryFind "period" |? "week"
            let dateFrom, dateTo = periodToDateRange period

            let! channel = getChannel context
            let! user =
                twitchService.GetUser channel
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            match! twitchService.GetClips user.Id dateFrom dateTo |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - Clips") with
            | [] -> return Message "No clips found"
            | clips ->
                let clip = clips |> Seq.randomChoice
                return Message $""""{clip.Title}" - clipped on {clip.CreatedAt.ToString(DateStringFormat)}, {clip.Duration} secs, {clip.ViewCount.ToString("N0")} views - {clip.Url}"""
        }
