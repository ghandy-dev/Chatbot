namespace Chatbot.Commands.Api

module Nasa =

    open Chatbot.Commands.Types.Nasa
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    [<Literal>]
    let private apiUrl = "https://api.nasa.gov"

    let private apiKey = Chatbot.Configuration.Nasa.config.ApiKey

    let private pictureOfTheDay date = $"{apiUrl}/planetary/apod?api_key={apiKey}&date={date}"

    let private marsRoverPhotos date camera = $"{apiUrl}/mars-photos?api_key={apiKey}&date={date}&camera={camera}"

    let dateFormat = "yyyy-MM-dd"

    let private userAgent = configuration.Item("UserAgent")

    let private getFromJsonAsync<'a> url =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                    UserAgent userAgent

                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! posts = response |> deserializeJsonAsync<'a>
                return Ok posts
            | Error e -> return Error $"Http response did not indicate success. {(int) e.statusCode} {e.reasonPhrase}"
        }

    let getPictureOfTheDay (date: DateOnly option) =
        async {
            let date = date |> Option.defaultValue (DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime))
            let url = pictureOfTheDay (date.ToString(dateFormat))

            return! getFromJsonAsync<APOD> url
        }

    let getMarsRoverPhoto (camera: RoverCamera) (date: DateOnly option) =
        async {
            let date = date |> Option.defaultValue (DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime))
            let url = marsRoverPhotos date camera
            return! getFromJsonAsync<APOD> url
        }

