module OpenAI

open System
open System.Collections.Concurrent
open System.Net.Http
open System.Text.Json.Serialization

open FSharpPlus
open FsToolkit.ErrorHandling

open Configuration
open Http
open Json

module Chat =

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

open Chat
open Image

let [<Literal>] private ApiUrl = "https://api.openai.com/v1"

let private imageGenerationUrl = $"{ApiUrl}/images/generations"
let private chatCompletionUrl = $"{ApiUrl}/chat/completions"

let private apiKey = appConfig.OpenAI.ApiKey
let private headers = [ Header.accept ContentType.ApplicationJson ; Header.authorization <| AuthenticationScheme.bearer apiKey ]

let getImage (prompt: string) =
    async {
        let json =
            { Model = appConfig.OpenAI.DefaultImageModel
              Prompt = prompt
              n = 1
              Size = "1024x1024" }
            |> serializeJson

        let request =
            Request.post imageGenerationUrl
            |> Request.withHeaders headers
            |> Request.withBody (Content.String json)
            |> Request.withContentType ContentType.ApplicationJson

        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<GenerateImageResponse>
            |> Result.mapError _.StatusCode
    }

let sendGptMessage (messages: TextGenerationMessage list) =
    async {
        let json =
            { Model = appConfig.OpenAI.DefaultChatModel
              Messages = messages
              n = 1
              Verbosity = "low"
              ReasoningEffort = "minimal" }
            |> serializeJson

        let request =
            Request.post chatCompletionUrl
            |> Request.withHeaders headers
            |> Request.withBody (Content.String json)
            |> Request.withContentType ContentType.ApplicationJson

        let! response = request |> Http.send Http.client

        return
            response
            |> Response.toJsonResult<TextGenerationMessageResponse>
            |> Result.mapError _.StatusCode
    }
