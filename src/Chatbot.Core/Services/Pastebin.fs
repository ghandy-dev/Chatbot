module Pastebin

open FsToolkit.ErrorHandling

open Configuration
open Http

type PasteExpireDate =
    | Never
    | TenMinutes
    | Hour
    | Day
    | Week
    | TwoWeeks
    | Month
    | SixMonths
    | Year
    with

        override this.ToString () =
            match this with
            | Never -> "N"
            | TenMinutes -> "10M"
            | Hour -> "1H"
            | Day -> "1D"
            | Week -> "1W"
            | TwoWeeks -> "2W"
            | Month -> "1M"
            | SixMonths -> "6M"
            | Year -> "1Y"

type PasteVisibility =
    | Public
    | Unlisted
    | Private
    with

        override this.ToString () =
            match this with
            | Public -> "0"
            | Unlisted -> "1"
            | Private -> "2"


let [<Literal>] private apiUrl = "https://pastebin.com/api"

let private createPasteUrl = $"{apiUrl}/api_post.php"

let private apiKey = appConfig.Pastebin.ApiKey

let createPaste (pasteName: string) (pasteCode: string) =
    async {
        let url = createPasteUrl
        let apiOption = "paste"
        let pastePrivate = Unlisted
        let pasteExpireDate = Week

        let parameters = [
            "api_dev_key", apiKey
            "api_option", apiOption
            "api_paste_code", pasteCode
            "api_paste_private", pastePrivate.ToString()
            "api_paste_name", pasteName
            "api_paste_expire_date", pasteExpireDate.ToString()
        ]

        let request =
            Request.post url
            |> Request.withBody (Content.FormUrlEncoded parameters)
            |> Request.withContentType ContentType.applicationFormUrlEncoded

        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap _.Content _.StatusCode
    }
