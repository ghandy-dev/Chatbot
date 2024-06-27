namespace Chatbot.Commands.Api

module DallE =

    open Chatbot.Commands.Types.DallE
    open Chatbot.Configuration

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    [<Literal>]
    let private apiUrl = "https://api.openai.com/v1"

    let private imageGeneration = $"{apiUrl}/images/generation"

    // Untested - DallE API documentation lacks model definitions (particularly the response model)
    let private getFromJsonAsync<'a> request url =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.applicationJson
                    AuthorizationBearer DallE.config.ApiKey
                    body
                    jsonSerialize request
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error e -> return Error $"Http response did not indicate success. {(int)e.statusCode} {e.reasonPhrase}"
        }

    let getImage size prompt =
        async {
            let request = {
                model = "dall-e-3"
                prompt = prompt
                n = 1
                size = size
            }
            return! getFromJsonAsync<GenerateImageResponse> request imageGeneration
        }
