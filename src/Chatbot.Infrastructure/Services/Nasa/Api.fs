namespace Chatbot.Infrastructure.Services

open System

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.Nasa.Types

type NasaService =
    abstract member GetCurrentPictureOfTheDay: unit -> Async<Result<APOD, int>>
    abstract member GetPictureOfTheDay: date: DateOnly -> Async<Result<APOD, int>>
    abstract member GetMarsRoverPhoto: camera: RoverCamera -> date: DateOnly -> Async<Result<APOD, int>>

type NasaOptions = {
    ApiKey: string
}

module NasaService =

    let create env config =

        let apiUrl = "https://api.nasa.gov"

        let apiKey = config.ApiKey

        let httpClient = env.HttpClient

        let currentPictureOfTheDay =
            UrlBuilder.buildUrl
                $"{apiUrl}/planetary/apod"
                [ "api_key", apiKey ]

        let pictureOfTheDay date =
            UrlBuilder.buildUrl
                $"{apiUrl}/planetary/apod"
                [ "api_key", apiKey ; "date", date ]

        let marsRoverPhotos (date: string) camera =
            UrlBuilder.buildUrl
                $"{apiUrl}/mars-photos"
                [ "api_key", apiKey ; "date", date ; "camera", camera ]

        let dateFormat = "yyyy-MM-dd"

        let getCurrentPictureOfTheDay () =
            async {
                let url = currentPictureOfTheDay
                let request = Request.get url
                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<APOD>
                    |> Result.mapError _.StatusCode
            }

        let getPictureOfTheDay (date: DateOnly) =
            async {
                let url = pictureOfTheDay (date.ToString(dateFormat))
                let request = Request.get url
                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<APOD>
                    |> Result.mapError _.StatusCode
            }

        let getMarsRoverPhoto (camera: RoverCamera) (date: DateOnly) =
            async {
                let url = marsRoverPhotos (date.ToString(dateFormat)) (camera |> RoverCamera.toString)
                let request = Request.get url
                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<APOD>
                    |> Result.mapError _.StatusCode
            }

        {
            new NasaService with
                member _.GetCurrentPictureOfTheDay () = getCurrentPictureOfTheDay ()
                member _.GetPictureOfTheDay date = getPictureOfTheDay date
                member _.GetMarsRoverPhoto camera date = getMarsRoverPhoto camera date
        }