namespace Commands

[<AutoOpen>]
module Nasa =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Nasa.Api
    open Nasa.Types

    let apod context =
        asyncResult {
            let! apod =
                match context.Args with
                | [] ->
                    getCurrentPictureOfTheDay ()
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Nasa")
                | args ->
                    async.Return (
                        Parsing.tryParseDateOnly (args |> String.concat " ") |> Option.toResultWith (InvalidArgs "Couldn't parse date")
                    )
                    |> AsyncResult.bind (
                        getPictureOfTheDay >> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Nasa")
                    )

            let url = apod.HdUrl |> Option.defaultValue  apod.Url

            return Message $"%s{apod.Title} %s{url}"
        }
