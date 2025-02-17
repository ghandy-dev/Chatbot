namespace Nasa

module Types =

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