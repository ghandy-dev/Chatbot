namespace Commands

[<AutoOpen>]
module GptImage =

    open FsToolkit.ErrorHandling

    open CommandError

    let private openAiService = Services.services.OpenAiService
    let private imageUploadService = Services.services.ImageUploadService

    let gptImage args =
        asyncResult {
            match args with
            | [] -> return! invalidArgs $"No prompt provided"
            | _ ->
                let prompt = args |> String.concat " "
                let! response = openAiService.GetImage prompt |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")

                match response.Data with
                | [] -> return Message "No image generated..."
                | d :: _ ->
                    let bytes = System.Convert.FromBase64String(d.B64Json)
                    let! url = imageUploadService.Upload(bytes) |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "nuuls")

                    return Message $"Generated image: {url}"
        }
