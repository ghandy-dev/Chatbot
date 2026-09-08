namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Infrastructure.Services.Pastebin.Types

type PasteBinOptions = {
    ApiKey: string
}

type ITextStorageService =
    abstract member CreatePost: title: string -> contents: string -> Async<Result<string, int>>

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