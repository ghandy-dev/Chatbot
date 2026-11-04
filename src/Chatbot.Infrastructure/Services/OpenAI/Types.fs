namespace Chatbot.Infrastructure.Services.OpenAI.Types

[<RequireQualifiedAccess>]
module Constants =

    let [<Literal>] InputText = "input_text"
    let [<Literal>] OutputText = "output_text"


namespace Chatbot.Infrastructure.Services.OpenAI.Types.Chat

open System.Text.Json.Serialization

type ResponseRequest = {
    Model: string
    Input: ResponseRequestInput list
    Tools: ResponseRequestTool list
    Reasoning: ResponseRequestReasoning
}

and ResponseRequestInput = {
    Role: string
    Content: ChatMessageContent list
}

and ChatMessageContent = {
    Type: string
    Text: string
}

and ResponseRequestTool = {
    Type: string
}

and ResponseRequestReasoning = {
    Effort: string
}

type ResponseResult = {
    Id: string
    [<JsonPropertyName("created_at")>]
    CreatedAt: int
    [<JsonPropertyName("completed_at")>]
    CompletedAt: int
    Model: string
    Object: string
    Usage: ResponseTokenUsage
    Status: string
    ToolChoice: string
    Output: ResponseItem list
}

and ResponseItem = {
    Id: string
    Content: ChatMessageContent list
    Role: string
    Status: string
    Type: string
}

and ResponseTokenUsage = {
    [<JsonPropertyName("input_tokens")>]
    InputTokens: int
    [<JsonPropertyName("output_tokens")>]
    OutputTokens: int
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
