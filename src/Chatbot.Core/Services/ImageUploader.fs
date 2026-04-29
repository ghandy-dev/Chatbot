namespace Chatbot.Core.Services


module ImageUpload =

    open FsToolkit.ErrorHandling

    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types

    type IImageUploadService =
        abstract member Upload: byte array -> Async<Result<string, int>>

    module ImageUploadService =

        let create env =

            let apiUrl = "https://i.nuuls.com"
            let uploadUrl = $"{apiUrl}/upload"

            let httpClient = env.HttpClient

            let upload bytes =
                async {
                    let content = seq {
                        "name", Content.string ""
                        "file", Content.file "image.png" ContentType.ImagePng bytes
                    }

                    let request =
                        Request.post uploadUrl
                        |> Request.withBody (Content.MultipartFormData content)

                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toResult
                        |> Result.eitherMap _.Content _.StatusCode
                }

            {
                new IImageUploadService with
                    member _.Upload bytes = upload bytes
            }