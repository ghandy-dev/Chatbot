namespace Chatbot.Commands

[<AutoOpen>]
module Gpt =

    open System.Collections.Concurrent

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Common.Parsing
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain
    open Chatbot.Infrastructure.Services
    open Chatbot.Infrastructure.Services.OpenAI.Types
    open Chatbot.Infrastructure.Services.OpenAI.Types.Chat

    type MessageHistory = {
        LastMessageTimestamp: System.DateTimeOffset
        Messages: ResponseRequestInput list
        LastMessage: string
        ContinueMessageIndex: int
    }

    let private systemMessage = {
        Role = "system"
        Content = [
            {
                Type = Constants.InputText
                Text = "You are a friendly and knowledgeable assistant. Provide brief and clear responses. Respond using plaintext."
            }
        ]
    }

    let private userChatHistory = new ConcurrentDictionary<string, MessageHistory>()

    let private keys = [ "continue" ; "websearch" ]

    let [<Literal>] private MessageInterval = 497

    let gpt (genAIService: IGenAIService) context =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "Gpt currently cannot be used in whispers"
            | Channel (channel, _) ->
                let parserResult = KeyValueParser.parse context.MessageArgs keys

                let continueLastMessage =
                    parserResult.KeyValues
                    |> Map.tryFind "continue"
                    |> Option.bind tryParseBoolean

                let useWebSearch =
                    parserResult.KeyValues
                    |> Map.tryFind "websearch"
                    |> Option.bind tryParseBoolean
                    |> Option.defaultValue false

                let chatHistoryKey = $"{context.Username}_{channel}"

                match continueLastMessage with
                | None
                | Some false ->
                    match parserResult.Input with
                    | [] -> return! invalidArgs "No input provided"
                    | args ->
                        let message = args |> String.join " "

                        let messages =
                            let message = [
                                {
                                    Role = "user"
                                    Content = [
                                        {
                                            Type = Constants.InputText
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
                            genAIService.SendGptMessage messages useWebSearch
                            |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")
                            |> AsyncResult.map (fun response ->
                                match response.Output |> List.tryLast with
                                | None -> "No response..."
                                | Some { Content = [] } -> "No response message..."
                                | Some { Content = message :: _ ; Role = role } ->
                                    let messageReplay =
                                        List.append messages [
                                            {
                                                Role = role
                                                Content = [
                                                    {
                                                        Type = message.Type
                                                        Text = message.Text
                                                    }
                                                ]
                                            }
                                        ]

                                    let message = message.Text |> stripMarkdownTags |> String.replace "  " " "

                                    userChatHistory[chatHistoryKey] <- {
                                        LastMessageTimestamp = utcNow ()
                                        Messages = messageReplay
                                        ContinueMessageIndex = MessageInterval
                                        LastMessage = message
                                    }

                                    message
                            )

                        return [ Message responseMessage ]
                | Some true ->
                    match userChatHistory |> Dict.tryGetValue chatHistoryKey with
                    | Some messages when (utcNow () - messages.LastMessageTimestamp).TotalMinutes <= 10 ->
                        let lastMessage = messages.LastMessage

                        if lastMessage.Length < messages.ContinueMessageIndex then
                            return [ Message "End of message reached" ]
                        else
                            userChatHistory[chatHistoryKey] <- {
                                messages with
                                    LastMessageTimestamp = utcNow ()
                                    ContinueMessageIndex = messages.ContinueMessageIndex + MessageInterval
                            }

                            return [ Message lastMessage[messages.ContinueMessageIndex..] ]
                    | _ -> return [ Message "No message to continue" ]
        }
