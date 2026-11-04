namespace Chatbot.Infrastructure.Services

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Common.Json
open Chatbot.Infrastructure.Services.OpenAI.Types.Chat
open Chatbot.Infrastructure.Services.OpenAI.Types.Image

type OpenAIOptions = {
    ApiKey: string
    DefaultImageModel: string
    DefaultChatModel: string
}

type IGenAIService =
    abstract member GetImage: prompt: string -> Async<Result<GenerateImageResponse, int>>
    abstract member SendGptMessage: messages: ResponseRequestInput list -> useWebSearch: bool -> Async<Result<ResponseResult, int>>

module OpenAIService =

    let create env config =

        let apiUrl = "https://api.openai.com"

        let imageGenerationUrl = $"{apiUrl}/v1/images/generations"
        let responsesUrl = $"{apiUrl}/v1/responses"

        let apiKey = config.ApiKey
        let defaultImageModel = config.DefaultImageModel
        let defaultChatModel = config.DefaultChatModel

        let httpClient = env.HttpClient

        let getImage (prompt: string) =
            async {
                let json =
                    {
                        Model = defaultImageModel
                        Prompt = prompt
                        n = 1
                        Size = "1024x1024"
                    }
                    |> serializeJson

                let request =
                    Request.post imageGenerationUrl
                    |> Request.withHeaders [ Header.accept ContentType.ApplicationJson ; Header.authorization <| AuthenticationScheme.bearer apiKey ]
                    |> Request.withBody (Content.String json)
                    |> Request.withContentType ContentType.ApplicationJson

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<GenerateImageResponse>
                    |> Result.mapError _.StatusCode
            }

        let sendGptMessage (messages: ResponseRequestInput list) (useWebSearch: bool) =
            async {
                let json =
                    {
                        Model = defaultChatModel
                        Reasoning = {
                            Effort = "low"
                        }
                        Tools = [ if useWebSearch then { Type = "web_search" } ]
                        Input = messages
                    }
                    |> serializeJson

                let request =
                    Request.post responsesUrl
                    |> Request.withHeaders [ Header.accept ContentType.ApplicationJson ; Header.authorization <| AuthenticationScheme.bearer apiKey ]
                    |> Request.withBody (Content.String json)
                    |> Request.withContentType ContentType.ApplicationJson

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<ResponseResult>
                    |> Result.mapError _.StatusCode
            }

        {
            new IGenAIService with
                member _.GetImage prompt = getImage prompt
                member _.SendGptMessage messages useWebSearch = sendGptMessage messages useWebSearch
        }