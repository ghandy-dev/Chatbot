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
    abstract member SendGptMessage: messages: TextGenerationMessage list -> Async<Result<TextGenerationMessageResponse, int>>

module OpenAIService =

    let create env config =

        let apiUrl = "https://api.openai.com/v1"

        let imageGenerationUrl = $"{apiUrl}/images/generations"
        let chatCompletionUrl = $"{apiUrl}/chat/completions"

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

        let sendGptMessage (messages: TextGenerationMessage list) =
            async {
                let json =
                    {
                        Model = defaultChatModel
                        Messages = messages
                        n = 1
                        Verbosity = "low"
                        ReasoningEffort = "low"
                    }
                    |> serializeJson

                let request =
                    Request.post chatCompletionUrl
                    |> Request.withHeaders [ Header.accept ContentType.ApplicationJson ; Header.authorization <| AuthenticationScheme.bearer apiKey ]
                    |> Request.withBody (Content.String json)
                    |> Request.withContentType ContentType.ApplicationJson

                let! response = request |> Http.send httpClient

                return
                    response
                    |> Response.toJsonResult<TextGenerationMessageResponse>
                    |> Result.mapError _.StatusCode
            }

        {
            new IGenAIService with
                member _.GetImage prompt = getImage prompt
                member _.SendGptMessage messages = sendGptMessage messages
        }