namespace Chatbot.Core.Services

module OpenAI =

    open System.Text.Json.Serialization

    module Chat =

        module Types =

            type TextGeneration = {
                Model: string
                Messages: TextGenerationMessage list
                n: int
                Verbosity: string
                [<JsonPropertyName("reasoning_effort")>]
                ReasoningEffort: string
            }

            and TextGenerationMessage = {
                Role: string
                Name: string option
                Content: MessageContent list
            }

            and MessageContent = {
                Type: string
                Text: string
            }

            type TextGenerationMessageResponse = {
                Id: string
                Choices: Choices list
                Created: int
                Model: string
                [<JsonPropertyName("service_tier")>]
                ServiceTier: string option
                [<JsonPropertyName("system_fingerprint")>]
                SystemFingerprint: string
                Object: string
                Usage: TokenUsage
            }

            and Choices = {
                Index: int
                Message: TextGenerationResponseMessage
            }

            and TextGenerationResponseMessage = {
                Role: string
                Content: string
            }

            and TokenUsage = {
                [<JsonPropertyName("prompt_tokens")>]
                PromptTokens: int
                [<JsonPropertyName("completion_tokens")>]
                CompletionTokens: int
                [<JsonPropertyName("total_tokens")>]
                TotalTokens: int
            }

    module Image =

        module Types =

            type GenerateImage = {
                Model: string
                Prompt: string
                n: int
                Size: string
            }

            type GenerateImageResponse = {
                Background: string
                Created: int
                Data: ImageData list
                [<JsonPropertyName("output_format")>]
                OutputFormat: string
                Quality: string
                Size: string
                Usage: TokenUsage
            }

            and ImageData = {
                [<JsonPropertyName("b64_json")>]
                B64Json: string
            }

            and TokenUsage = {
                [<JsonPropertyName("input_tokens")>]
                InputTokens: int
                [<JsonPropertyName("input_token_details")>]
                InputTokenDetails: InputTokenDetails
                [<JsonPropertyName("output_tokens")>]
                OutputTokens: int
                [<JsonPropertyName("total_tokens")>]
                TotalTokens: int
            }

            and InputTokenDetails = {
                [<JsonPropertyName("text_tokens")>]
                TextTokens: int
                [<JsonPropertyName("image_tokens")>]
                ImageTokens: int
            }

    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types
    open Chatbot.Core.Json
    open Chat.Types
    open Image.Types

    type IGenAIService =
        abstract member GetImage: prompt: string -> Async<Result<GenerateImageResponse, int>>
        abstract member SendGptMessage: messages: TextGenerationMessage list -> Async<Result<TextGenerationMessageResponse, int>>

    type OpenAIOptions = {
        ApiKey: string
        DefaultImageModel: string
        DefaultChatModel: string
    }

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