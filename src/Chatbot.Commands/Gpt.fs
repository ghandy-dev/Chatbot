namespace Commands

[<AutoOpen>]
module Gpt =

    open System.Collections.Concurrent

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open CommandError
    open OpenAI.Chat
    open Parsing

    type MessageHistory = {
        LastMessageTimestamp: System.DateTime
        Messages: TextGenerationMessage list
        LastMessage: string
        ContinueMessageIndex: int
    }

    let private openAiService = Services.openAiService

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

    let private userChatHistory = new ConcurrentDictionary<string, MessageHistory>()

    let private keys = [ "continue" ]

    let [<Literal>] private MessageInterval = 497

    let gpt context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "Gpt currently cannot be used in whispers"
            | Channel channel ->
                let parserResult = KeyValueParser.parse context.Args keys

                let continueLastMessage =
                    parserResult.KeyValues
                    |> Map.tryFind "continue"
                    |> Option.bind tryParseBoolean

                let chatHistoryKey = $"{context.Username}_{channel}"

                match continueLastMessage with
                | None
                | Some false ->
                    match parserResult.Input with
                    | [] -> return! invalidArgs "No input provided"
                    | args ->
                        let message = args |> String.concat " "

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

                            match userChatHistory |> Dict.tryGetValue chatHistoryKey with
                            | None -> systemMessage :: message
                            | Some messages when (utcNow () - messages.LastMessageTimestamp).TotalMinutes > 10 ->
                                let updatedMessages = systemMessage :: message
                                updatedMessages
                            | Some messages ->
                                let updatedMessages = messages.Messages @ message
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

                                    let message = choice.Message.Content |> stripMarkdownTags |> strReplace "  " " "

                                    userChatHistory[chatHistoryKey] <- {
                                        LastMessageTimestamp = utcNow ()
                                        Messages = messages
                                        ContinueMessageIndex = MessageInterval
                                        LastMessage = message
                                    }

                                    message
                            )

                        return Message responseMessage
                | Some true ->
                    match userChatHistory |> Dict.tryGetValue chatHistoryKey with
                    | Some messages when (utcNow () - messages.LastMessageTimestamp).TotalMinutes <= 10 ->
                        let lastMessage = messages.LastMessage

                        if lastMessage.Length < messages.ContinueMessageIndex then
                            return Message "End of message reached"
                        else
                            userChatHistory[chatHistoryKey] <- {
                                messages with
                                    LastMessageTimestamp = utcNow ()
                                    ContinueMessageIndex = messages.ContinueMessageIndex + MessageInterval
                            }

                            return Message lastMessage[messages.ContinueMessageIndex..]
                    | _ -> return Message "No message to continue"
        }
