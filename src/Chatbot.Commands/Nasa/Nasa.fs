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
            | false, _ -> Error $"Couldn't parse provided date. Expected format: YYYY-MM-DD"
            | true, parsedDate -> Ok <| Some parsedDate

    let apod args =
        async {
            match! parseApodArgs args |> Async.create |> AsyncResult.bind getPictureOfTheDay with
            | Error err -> return Error err
            | Ok apod -> return Ok <| Message $"{apod.Title} {apod.HdUrl}"
        }
