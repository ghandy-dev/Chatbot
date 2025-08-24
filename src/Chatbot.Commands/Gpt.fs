namespace Commands

[<AutoOpen>]
module Gpt =

    open System.Collections.Concurrent

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open CommandError
    open OpenAI.Chat

    type MessageHistory = {
        LastMessage: System.DateTime
        Messages: TextGenerationMessage list
    }

    let [<Literal>] DefaultModelKey = "default"
    let [<Literal>] ChatSummaryKey = "chatSummary"

    let private openAiService = Services.openAiService

    let private systemMessage =
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

    let private userChatHistory = new ConcurrentDictionary<string, MessageHistory>()

    let gpt args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "Gpt currently cannot be used in whispers"
            | Channel channel ->
                match args with
                | [] -> return! invalidArgs "No input provided"
                | _ ->
                    let message = args |> String.concat " "
                    let historyKey = $"{context.Username}_{channel}"

                    let messages =
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

                        match userChatHistory |> Dict.tryGetValue historyKey  with
                        | None -> systemMessage :: message
                        | Some messages when (utcNow() - messages.LastMessage).TotalMinutes > 10 ->
                            let updatedMessages = systemMessage :: message

                            userChatHistory[historyKey] <- {
                                LastMessage = utcNow()
                                Messages = updatedMessages
                            }

                            updatedMessages
                        | Some messages ->
                            let updatedMessages = messages.Messages @ message

                            userChatHistory[historyKey] <- {
                                LastMessage = utcNow()
                                Messages = updatedMessages
                            }

                            updatedMessages

                    let! responseMessage =
                        openAiService.SendGptMessage messages
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")
                        |> AsyncResult.map (fun response ->
                            match response.Choices with
                            | [] -> "No response message..."
                            | choice :: _ ->
                                let messages =
                                    List.append messages [
                                        {
                                            Role = choice.Message.Role
                                            Name = None
                                            Content = [
                                                {
                                                    Type = "text"
                                                    Text = choice.Message.Content
                                                }
                                            ]
                                        }
                                    ]

                                userChatHistory[historyKey] <- {
                                    LastMessage = utcNow()
                                    Messages = messages
                                }

                                choice.Message.Content
                                |> stripMarkdownTags
                            )

                    return Message responseMessage
        }
