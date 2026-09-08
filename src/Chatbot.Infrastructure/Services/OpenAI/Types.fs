namespace Chatbot.Infrastructure.Services.OpenAI.Types

namespace Chatbot.Infrastructure.Services.OpenAI.Types.Chat

    open System.Text.Json.Serialization

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

namespace Chatbot.Infrastructure.Services.OpenAI.Types.Image

    open System.Text.Json.Serialization

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
