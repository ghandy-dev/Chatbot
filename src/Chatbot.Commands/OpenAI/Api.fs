namespace Chatbot.Commands.OpenAI

module Api =

    open Chatbot.Configuration
    open Types.DallE
    open Types.Gpt

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System

    type UserChatHistory = {
        LastMessage: DateTime
        Messages: TextGenerationMessage list
    }

    let userChatHistory =
        new Collections.Concurrent.ConcurrentDictionary<string, UserChatHistory>()

    let [<Literal>] private apiUrl = "https://api.openai.com/v1"

    let private imageGeneration = $"{apiUrl}/images/generation"
    let private chatCompletion = $"{apiUrl}/chat/completions"

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
                let! data = response |> deserializeJsonAsync<'a>
                return Ok data
            | Error e -> return Error $"Http response did not indicate success. {(int) e.statusCode} {e.reasonPhrase}"
        }

    let getImage size prompt =
        async {
            let request = {
                Model = DallE3
                Prompt = prompt
                n = 1
                Size = size
            }

            match! postAsJson<GenerateImageResponse, GenerateImage> imageGeneration request with
            | Error err -> return Error err
            | Ok response -> return Ok response.Url
        }

    let private systemMessage = {
        Role = "system"
        Name = None
        Content = [
            {
                Type = "text"
                Text = "You are a friendly and knowledgeable assistant. Provide brief and clear responses. Respond using plaintext."
            }
        ]
    }

    let private evilSystemMessage = {
        Role = "system"
        Name = None
        Content = [
            {
                Type = "text"
                Text = "You are a character who is mean and a bully. Your tone is condescending, sarcastic, and occasionally mocking. You enjoy teasing and belittling others in a playful, non-toxic way."
            }
        ]
    }

    let sendGptMessage message user channel evil =
        async {
            let evilKey = if evil then "evil" else ""
            let key = $"{user}_{channel}_{evilKey}"
            let systemMessage = if evil then evilSystemMessage else systemMessage

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
                match userChatHistory.TryGetValue key with
                | false, _ -> systemMessage :: message
                | true, messages when (DateTime.UtcNow - messages.LastMessage).TotalMinutes > 10 ->
                    let updatedMessages = systemMessage :: message

                    userChatHistory[key] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = updatedMessages
                    }

                    updatedMessages
                | true, messages ->
                    let updatedMessages = messages.Messages @ message

                    userChatHistory[key] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = updatedMessages
                    }

                    updatedMessages

            let request = {
                Model = Chatbot.Configuration.OpenAI.config.DefaultModel
                Messages = messages
                MaxTokens = 150
                n = 1
                User = key
            }

            match! postAsJson<TextGenerationMessageResponse, TextGeneration> chatCompletion request with
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

                    userChatHistory[key] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = messages
                    }

                    return Ok message.Content
        }
