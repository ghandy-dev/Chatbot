namespace Chatbot.Commands

[<AutoOpen>]
module GptImage =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError

    open Chatbot.Core.Services.ImageUpload
    open Chatbot.Core.Services.OpenAI

    let gptImage (genAIService: IGenAIService) (imageUploadService: IImageUploadService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! invalidArgs $"No prompt provided"
            | _ ->
                let prompt = context.MessageArgs |> String.join " "
                let! response = genAIService.GetImage prompt |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")

                match response.Data with
                | [] -> return [ Message "No image generated..." ]
                | d :: _ ->
                    let bytes = System.Convert.FromBase64String(d.B64Json)
                    let! url = imageUploadService.Upload(bytes) |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "nuuls")

                    return [ Message $"Generated image: {url}" ]
        }
