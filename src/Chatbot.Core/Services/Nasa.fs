module Nasa

open System
open System.Text.Json.Serialization

// Astronomy Picture of the Day
type APOD = {
    Copyright: string
    Date: DateOnly
    Explanation: string
    HdUrl: string option
    [<JsonPropertyName("media_type")>]
    MediaType: string
    [<JsonPropertyName("service_version")>]
    ServiceVersion: string
    Title: string
    Url: string
}

type RoverCamera =
    // Front Hazard Avoidance Camera
    | FHAZ
    // Rear Hazard Avoidance Camera
    | RHAZ
    // Mast Camera
    | MAST
    // Chemistry and Camera Complex
    | CHEMCAM
    // Mars Hand Lens Imager
    | MAHLI
    // Mars Descent Imager
    | MARDI
    // Navigation Camera
    | NAVCAM
    // Panoramic Camera
    | PANCAM
    // Miniature Thermal Emission Spectrometer (Mini-TES)
    | MINITES

    static member tryParse s =
        match s with
        | "FHAZ" -> Some FHAZ
        | "RHAZ" -> Some RHAZ
        | "MAST" -> Some MAST
        | "CHEMCAM" -> Some CHEMCAM
        | "MAHLI" -> Some MAHLI
        | "MARDI" -> Some MARDI
        | "NAVCAM" -> Some NAVCAM
        | "PANCAM" -> Some PANCAM
        | "MINITES" -> Some MINITES
        | _ -> None

    static member toString s =
        match s with
        | FHAZ -> "FHAZ"
        | RHAZ -> "RHAZ"
        | MAST -> "MAST"
        | CHEMCAM -> "CHEMCAM"
        | MAHLI -> "MAHLI"
        | MARDI -> "MARDI"
        | NAVCAM -> "NAVCAM"
        | PANCAM -> "PANCAM"
        | MINITES -> "MINITES"

type MarsPhotos = {
    Photos: MarsePhoto list
}

and MarsePhoto = {
    Id: int
    Sol: int
    Camera: Camera
    [<JsonPropertyName("img_src")>]
    ImgSrc: string
    [<JsonPropertyName("earth_date")>]
    EarthDate: DateOnly
    Rover: Rover
}

and Camera = {
    Id: int
    Name: string
    [<JsonPropertyName("rover_id")>]
    RoverId: int
    [<JsonPropertyName("full_name")>]
    FullName: string
}

and Rover = {
    Id: int
    Name: string
    [<JsonPropertyName("landing_date")>]
    LandingDate: DateOnly
    [<JsonPropertyName("launch_date")>]
    LaunchDate: DateOnly
    Status: string
    [<JsonPropertyName("max_sol")>]
    MaxSol: int
    [<JsonPropertyName("max_date")>]
    MaxDate: DateOnly
    [<JsonPropertyName("total_photos")>]
    TotalPhotos: int
    Cameras: Camera2 list
}

and Camera2 = {
    Name: string
    [<JsonPropertyName("full_name")>]
    FullName: string
}

open System
open System.Net.Http

open FsToolkit.ErrorHandling

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
