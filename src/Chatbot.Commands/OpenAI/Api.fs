namespace OpenAI

module Api =

    open Configuration
    open Types.DallE
    open Types.Gpt

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System
    open System.Collections.Concurrent

    type UserChatHistory = {
        LastMessage: DateTime
        Messages: TextGenerationMessage list
    }

    let private userChatHistory = new ConcurrentDictionary<string, UserChatHistory>()

    let [<Literal>] private ApiUrl = "https://api.openai.com/v1"

    let private imageGenerationUrl = $"{ApiUrl}/images/generation"
    let private chatCompletionUrl = $"{ApiUrl}/chat/completions"

    // Untested - DallE API documentation lacks model definitions (particularly the response model)
    let private postAsJson<'a, 'b> url (request: 'b) =
        async {
            use! response =
                http {
                    POST url
                    Accept MimeTypes.applicationJson
                    AuthorizationBearer OpenAI.config.ApiKey
                    body
                    jsonSerialize request
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! deserialized = response |> deserializeJsonAsync<'a>
                return Ok deserialized
            | Error err -> return Error $"OpenAI API HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let getImage size prompt =
        async {
            let request = {
                Model = DallE3
                Prompt = prompt
                n = 1
                Size = size
            }

            match! postAsJson<GenerateImageResponse, GenerateImage> imageGenerationUrl request with
            | Error err -> return Error err
            | Ok response -> return Ok response.Url
        }

    let private systemMessages =
        [
            "default",
            {
                Role = "system"
                Name = None
                Content = [
                    {
                        Type = "text"
                        Text = "You are a friendly and knowledgeable assistant. Provide brief and clear responses. Respond using plaintext."
                    }
                ]
            }
            "bully",
            {
                Role = "system"
                Name = None
                Content = [
                    {
                        Type = "text"
                        Text = "You are a character who is mean and a bully. Your tone is condescending, sarcastic, and occasionally mocking. You enjoy teasing and belittling others in a playful, non-toxic way."
                    }
                ]
            }
        ] |> Map.ofList

    let sendGptMessage (message: string) (user: string) (channel: string) (modelKey: string) =
        async {
            let historyKey = $"{user}_{channel}_{modelKey}"
            let systemMessage = systemMessages |> Map.tryFind modelKey |?? (systemMessages |> Map.find "default")

            let message = [
                {
                    Role = "user"
                    Name = None
                    Content = [
                        {
                            Type = "text"
                            Text = message
                        }
                    ]
                }
            ]

            let messages =
                match userChatHistory.TryGetValue historyKey with
                | false, _ -> systemMessage :: message
                | true, messages when (DateTime.UtcNow - messages.LastMessage).TotalMinutes > 10 ->
                    let updatedMessages = systemMessage :: message

                    userChatHistory[historyKey] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = updatedMessages
                    }

                    updatedMessages
                | true, messages ->
                    let updatedMessages = messages.Messages @ message

                    userChatHistory[historyKey] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = updatedMessages
                    }

                    updatedMessages

            let request = {
                Model = Configuration.OpenAI.config.DefaultModel
                Messages = messages
                MaxTokens = 150
                n = 1
                User = historyKey
            }

            match! postAsJson<TextGenerationMessageResponse, TextGeneration> chatCompletionUrl request with
            | Error err -> return Error err
            | Ok response ->
                match response.Choices with
                | [] -> return Error "No response message"
                | choice :: _ ->
                    let message = choice.Message

                    let messages =
                        List.append messages [
                            {
                                Role = message.Role
                                Name = None
                                Content = [
                                    {
                                        Type = "text"
                                        Text = message.Content
                                    }
                                ]
                            }
                        ]

                    userChatHistory[historyKey] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = messages
                    }

                    return Ok message.Content
        }
