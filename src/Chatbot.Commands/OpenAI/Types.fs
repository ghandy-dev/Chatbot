namespace OpenAI

module Types =

    open System.Text.Json.Serialization

    module Gpt =

        let [<Literal>] Gpt3_5Turbo = "gpt-3.5-turbo"
        let [<Literal>] Gpt4 = "gpt-4o"

        type TextGeneration = {
            Model: string
            Messages: TextGenerationMessage list
            [<JsonPropertyName("max_tokens")>]
            MaxTokens: int
            n: int
            User: string
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

    module DallE =

        let [<Literal>] DallE3 = "dall-e-3"

        type GenerateImage = {
            Model: string
            Prompt: string
            n: int
            Size: string
        }

        type GenerateImageResponse = { Url: string }
