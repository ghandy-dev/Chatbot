namespace Chatbot.Commands

[<AutoOpen>]
module Nasa =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.Nasa

    let apod (nasaService: NasaService) context =
        asyncResult {
            let! apod =
                match context.MessageArgs with
                | [] ->
                    nasaService.GetCurrentPictureOfTheDay ()
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Nasa")
                | args ->
                    Parsing.tryParseDateOnly (args |> String.concat " ") |> Option.toResultWith (InvalidArgs "Couldn't parse date") |> async.Return
                    |> AsyncResult.bind (nasaService.GetPictureOfTheDay >> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Nasa"))

            let url = apod.HdUrl |> Option.defaultValue apod.Url

            return [ Message $"%s{apod.Title} %s{url}" ]
        }
