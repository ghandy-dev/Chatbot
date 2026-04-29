namespace Chatbot.Core.Services

module Pastebin =

    module Types =

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

                member this.toString =
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

                member this.toString =
                    match this with
                    | Public -> "0"
                    | Unlisted -> "1"
                    | Private -> "2"

    open FsToolkit.ErrorHandling

    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types
    open Types

    type ITextStorageService =
        abstract member CreatePost: title: string -> contents: string -> Async<Result<string, int>>

    type PasteBinOptions = {
        ApiKey: string
    }

    module PastebinService =

        let create env config =

            let apiUrl = "https://pastebin.com/api"
            let createPasteUrl = $"{apiUrl}/api_post.php"

            let httpClient = env.HttpClient
            let apiKey = config.ApiKey

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
                        "api_paste_private", pastePrivate.toString
                        "api_paste_name", pasteName
                        "api_paste_expire_date", pasteExpireDate.toString
                    ]

                    let request =
                        Request.post url
                        |> Request.withBody (Content.FormUrlEncoded parameters)
                        |> Request.withContentType ContentType.ApplicationFormUrlEncoded

                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toResult
                        |> Result.eitherMap _.Content _.StatusCode
                }

            {
                new ITextStorageService with
                    member _.CreatePost title content = createPaste title content
            }