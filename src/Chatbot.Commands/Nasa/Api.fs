namespace Chatbot.Commands.Api

module Nasa =

    open Chatbot.Commands.Types.Nasa
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    let private apiKey = Chatbot.Configuration.Nasa.config.ApiKey
    let private userAgent = configuration.Item("UserAgent")

    let [<Literal>] private ApiUrl = "https://api.nasa.gov"

    let private pictureOfTheDay date = $"{ApiUrl}/planetary/apod?api_key={apiKey}&date={date}"
    let private marsRoverPhotos date camera = $"{ApiUrl}/mars-photos?api_key={apiKey}&date={date}&camera={camera}"

    let private dateFormat = "yyyy-MM-dd"

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
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error err -> return Error $"NASA API HTTP error {err.statusCode |> int} {err.statusCode}"
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

