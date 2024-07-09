namespace Chatbot.Commands.DallE

module Types =

    module Text =

        [<Literal>]
        let Gpt3_5Turbo = "gpt-3.5-turbo"

        [<Literal>]
        let Gpt4 = "gpt-4o"

        type TextGenerationMessage = {
            role: string
            content: string
        }

        type Choices = {
            index: int
            message: TextGenerationMessage
        }

        type TokenUsage = {
            prompt_tokens: int
            completion_tokens: int
            total_tokens: int
        }

        type TextGenerationMessageResponse = {
            id: string
            choices: Choices array
            created: int
            model: string
            service_tier: string option
            system_fingerprint: string
            object: string
            usage: TokenUsage
        }

        type TextGeneration = {
            model: string
            messages: TextGenerationMessage array
            max_tokens: int
            n: int
            user: string
        }

    module Image =

        [<Literal>]
        let DallE3 = "dall-e-3"

        type GenerateImage = {
            model: string
            prompt: string
            n: int
            size: string
        }

        type GenerateImageResponse = { url: string }
