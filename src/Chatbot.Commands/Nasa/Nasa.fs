namespace Commands

[<AutoOpen>]
module Nasa =

    open System

    open Commands.Api.Nasa
    open Commands.Types.Nasa

    let private parseApodArgs (args: string list) =
        match args with
        | [] -> Ok None
        | date :: _ ->
            match DateOnly.TryParse(date) with
            | false, _ -> Error $"Couldn't parse provided date"
            | true, parsedDate -> Ok <| Some parsedDate

    let apod args =
        async {
            match!
                parseApodArgs args
                |> Async.create
                |> Result.bindAsync getPictureOfTheDay
            with
            | Error err -> return Message err
            | Ok apod ->
                let url =
                    match apod.HdUrl with
                    | None -> apod.Url
                    | Some url -> url

                return Message $"{apod.Title} {url}"
        }
