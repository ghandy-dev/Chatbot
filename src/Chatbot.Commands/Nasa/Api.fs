namespace Nasa

module Api =

    open Types
    open Configuration

    open System

    let private apiKey = Configuration.Nasa.config.ApiKey
    let private userAgent = configuration.Item("UserAgent")

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

            match! Http.getFromJsonAsync<APOD> url with
            | Error(err, statusCode) ->
                Logging.error $"NASA API error: {err}" (new System.Net.Http.HttpRequestException("", null, statusCode))
                return None
            | Ok apod -> return Some apod
        }

    let getPictureOfTheDay (date: DateOnly) =
        async {
            let url = pictureOfTheDay (date.ToString(dateFormat))

            match! Http.getFromJsonAsync<APOD> url with
            | Error(err, statusCode) ->
                Logging.error $"NASA API error: {err}" (new System.Net.Http.HttpRequestException("", null, statusCode))
                return None
            | Ok apod -> return Some apod
        }

    let getMarsRoverPhoto (camera: RoverCamera) (date: DateOnly) =
        async {
            let url = marsRoverPhotos date camera

            match! Http.getFromJsonAsync<APOD> url with
            | Error(err, statusCode) ->
                Logging.error $"NASA API error: {err}" (new System.Net.Http.HttpRequestException("", null, statusCode))
                return None
            | Ok apod -> return Some apod
        }
