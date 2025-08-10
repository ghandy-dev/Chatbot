namespace OpenAI

module Api =

    open System
    open System.Collections.Concurrent
    open System.Net.Http

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Configuration
    open Http
    open Json
    open Types.DallE
    open Types.Gpt

    type UserChatHistory = {
        LastMessage: DateTime
        Messages: TextGenerationMessage list
    }

    let private userChatHistory = new ConcurrentDictionary<string, UserChatHistory>()

    let [<Literal>] private ApiUrl = "https://api.openai.com/v1"

    let private imageGenerationUrl = $"{ApiUrl}/images/generation"
    let private chatCompletionUrl = $"{ApiUrl}/chat/completions"

    let private apiKey = appConfig.OpenAI.ApiKey
    let private headers = [ Header.accept ContentType.applicationJson ; Header.authorization <| AuthenticationScheme.bearer apiKey ]

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

    let getImage size prompt =
        async {
            let json =
                { Model = DallE3
                  Prompt = prompt
                  n = 1
                  Size = size }
                |> serializeJson

            let request =
                Request.request imageGenerationUrl
                |> Request.withMethod Method.Post
                |> Request.withHeaders headers
                |> Request.withBody (Content.String json)
                |> Request.withContentType ContentType.applicationJson

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<GenerateImageResponse>
                |> Result.eitherMap _.Url _.StatusCode
        }

    let sendGptMessage (message: string) (user: string) (channel: string) (modelKey: string) =
        async {
            let historyKey = $"{user}_{channel}_{modelKey}"
            let systemMessage = systemMessages |> Map.tryFind modelKey |? (systemMessages |> Map.find "default")

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
                match userChatHistory |> Dict.tryGetValue historyKey  with
                | None -> systemMessage :: message
                | Some messages when (DateTime.UtcNow - messages.LastMessage).TotalMinutes > 10 ->
                    let updatedMessages = systemMessage :: message

                    userChatHistory[historyKey] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = updatedMessages
                    }

                    updatedMessages
                | Some messages ->
                    let updatedMessages = messages.Messages @ message

                    userChatHistory[historyKey] <- {
                        LastMessage = DateTime.UtcNow
                        Messages = updatedMessages
                    }

                    updatedMessages

            let json =
                { Model = appConfig.OpenAI.DefaultModel
                  Messages = messages
                  MaxCompletionTokens = 30000
                  n = 1
                  User = historyKey
                  Verbosity = "low"
                  ReasoningEffort = "low" }
                |> serializeJson

            let request =
                Request.request chatCompletionUrl
                |> Request.withMethod Method.Post
                |> Request.withHeaders headers
                |> Request.withBody (Content.String json)
                |> Request.withContentType ContentType.applicationJson

            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<TextGenerationMessageResponse>
                |> Result.mapError _.StatusCode
                |> Result.map (fun response ->
                    match response.Choices with
                    | [] -> "No response message"
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

                        message.Content
                )
        }
