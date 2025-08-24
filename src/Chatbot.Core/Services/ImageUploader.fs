module ImageUploader

open FsToolkit.ErrorHandling
open Http

let [<Literal>] private ApiUrl = "https://i.nuuls.com"
let private uploadUrl = $"{ApiUrl}/upload"

let upload bytes =
    async {
        let content = seq {
            "name", Content.string ""
            "file", Content.file "image.png" ContentType.ImagePng bytes
        }

        let request =
            Request.post uploadUrl
            |> Request.withBody (Content.MultipartFormData content)

        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toResult
            |> Result.eitherMap _.Content _.StatusCode
    }
