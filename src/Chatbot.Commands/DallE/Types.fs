namespace Chatbot.Commands.Types

module DallE =

    type GenerateImage = {
        model: string
        prompt: string
        n: int
        size: string
    }

    type GenerateImageResponse = {
        url: string
    }