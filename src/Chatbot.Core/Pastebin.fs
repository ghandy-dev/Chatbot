module Pastebin

open FsHttp
open FsHttp.Request
open FsHttp.Response

type private PasteExpireDate =
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

        override this.ToString() =
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

type private PasteVisibility =
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

let private apiKey = Chatbot.Configuration.Pastebin.config.ApiKey

let private post<'T> url bodyContent =
    async {
        use! response =
            http {
                POST url
                body
                formUrlEncoded bodyContent
            }
            |> sendAsync

        match response |> toResult with
        | Error err -> return Error $"Pastebin API HTTP error {err.statusCode |> int} {err.statusCode}"
        | Ok res ->
            let! r = res.content.ReadAsStringAsync() |> Async.AwaitTask
            return Ok r
    }

let createPaste (pasteName: string) (pasteCode: string) =
    async {
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

        return! post createPasteUrl parameters
    }
