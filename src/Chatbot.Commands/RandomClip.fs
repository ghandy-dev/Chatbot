namespace Chatbot.Commands

[<AutoOpen>]
module RandomClip =

    open System

    open TTVSharp.Helix
    open Utils

    let private getChannel args (context: Context) =
        match context.Source with
        | Whisper _ ->
            match args with
            | channel :: _ -> Ok channel
            | _ -> Error "You must provide a channel if using this command in whispers"
        | Channel channel ->
            match args with
            | [] -> Ok channel
            | channel :: _ -> Ok channel

    let private defaultKeyValues = Map [ ("period", "week") ]
    let keys = defaultKeyValues.Keys

    let private periodToDateRange period =
        let rangeTo = DateTime.Today

        let rangeFrom =
            match period with
            | "week" -> rangeTo.AddDays(-7)
            | "month" -> rangeTo.AddMonths(-1)
            | "year" -> rangeTo.AddYears(-1)
            | _ -> DateTime.MinValue

        (rangeFrom, rangeTo)

    let randomClip (args: string list) (context: Context) =
        async {
            let (args, map) =
                KeyValueParser.parseKeyValuePairs args (Some defaultKeyValues.Keys)

            let map = map |> Map.mergeInto defaultKeyValues
            let (dateFrom, dateTo) = periodToDateRange map["period"]

            match!
                Async.create (getChannel args context)
                |> AsyncResult.bind (Users.getUser >> AsyncResult.fromOption "User not found")
                |> AsyncResult.bind (fun user -> (Clips.getClips user.Id dateFrom dateTo))
            with
            | Ok clips ->
                match clips |> List.ofSeq with
                | [] -> return Ok <| Message "No clips found"
                | clips ->
                    let clip = clips[System.Random.Shared.Next(clips.Length)]

                    return
                        Ok
                        <| Message
                            $""""{clip.Title}" ({clip.ViewCount.ToString("N0")} views, {clip.CreatedAt.ToShortDateString()}) {clip.Url}"""
            | Error error -> return Error error
        }
