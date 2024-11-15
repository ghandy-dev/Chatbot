namespace Commands

[<AutoOpen>]
module Nasa =

    open System

    open Commands.Api.Nasa
    open Commands.Types.Nasa

    let private parseApodArgs (date: string) =
        match DateOnly.TryParse(date) with
        | false, _ -> None
        | true, parsedDate -> Some parsedDate

    let private formatApodMessage (apod: APOD) =
        let url =
            match apod.HdUrl with
            | None -> apod.Url
            | Some url -> url

        $"%s{apod.Title} %s{url}"

    let apod args =
        async {
            match args with
            | [] ->
                match! getCurrentPictureOfTheDay () with
                | None -> return Message "Couldn't get todays picture"
                | Some apod -> return Message (formatApodMessage apod)
            | args ->
                let maybeDate = parseApodArgs (args |> String.concat " ")
                match maybeDate with
                | None -> return Message "Couldn't parse date"
                | Some date ->
                    match! getPictureOfTheDay date with
                    | None -> return Message "Couldn't get todays picture"
                    | Some apod -> return Message (formatApodMessage apod)
        }
