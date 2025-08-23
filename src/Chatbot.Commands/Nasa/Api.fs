namespace Nasa

module Api =

    open System
    open System.Net.Http

    open FsToolkit.ErrorHandling

    open Types
    open Configuration
    open Http

    let private apiKey = appConfig.Nasa.ApiKey

    let [<Literal>] private ApiUrl = "https://api.nasa.gov"

    let private currentPictureOfTheDay = $"{ApiUrl}/planetary/apod?api_key={apiKey}"

    let private pictureOfTheDay date =
        $"{ApiUrl}/planetary/apod?api_key={apiKey}&date={date}"

    let private marsRoverPhotos date camera =
        $"{ApiUrl}/mars-photos?api_key={apiKey}&date={date}&camera={camera}"

    let private dateFormat = "yyyy-MM-dd"

    let getCurrentPictureOfTheDay () =
        async {
            let url = currentPictureOfTheDay
            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<APOD>
                |> Result.mapError _.StatusCode
        }

    let getPictureOfTheDay (date: DateOnly) =
        async {
            let url = pictureOfTheDay (date.ToString(dateFormat))
            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<APOD>
                |> Result.mapError _.StatusCode
        }

    let getMarsRoverPhoto (camera: RoverCamera) (date: DateOnly) =
        async {
            let url = marsRoverPhotos date camera
            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<APOD>
                |> Result.mapError _.StatusCode
        }
