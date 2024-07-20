namespace Chatbot.Commands

[<AutoOpen>]
module Nasa =

    open System

    open Chatbot.Commands.Api.Nasa
    open Chatbot.Commands.Types.Nasa

    let private parseApodArgs args =
        match args with
        | [] -> Ok None
        | date :: _ ->
            match DateOnly.TryParseExact(date, dateFormat) with
            | false, _ -> Error $"Couldn't parse provided date. Expected format: yyyy-mm-dd"
            | true, parsedDate -> Ok <| Some parsedDate

    let apod args =
        async {
            match!
                parseApodArgs args
                |> Async.create
                |> Result.bindAsync getPictureOfTheDay
            with
            | Error err -> return Error err
            | Ok apod ->
                let url =
                    match apod.HdUrl with
                    | None -> apod.Url
                    | Some url -> url

                return Ok <| Message $"{apod.Title} {url}"
        }
