namespace Chatbot.Commands

[<AutoOpen>]
module ChatSummary =

    open System.Collections.Concurrent

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain
    open Chatbot.Core.Services.OpenAI
    open Chatbot.Core.Services.OpenAI.Chat.Types
    open Chatbot.Core.Services.Ivr

    type SummaryCache = {
        LastMessage: System.DateTime
        Message: string
    }

    let [<Literal>] private MaxLines = 80

    let private systemMessage =
        {
            Role = "system"
            Name = None
            Content = [
                {
                    Type = "text"
                    Text = "Provide a brief summary of the twitch chat message logs, focusing on what is being discussed. Ignore any moderator actions (such as timeouts/bans), and ignore automated messages from chat bots. Respond using plain text."
                }
            ]
        }

    let private chatSummaryHistory = new ConcurrentDictionary<string, SummaryCache>()

    let chatSummary (ivrService: IvrService) (genAIService: IGenAIService) (context: Context) =
        asyncResult {
            match context.MessageSource with
            | Whisper _ -> return! invalidArgs "This command is only avaiable in channels"
            | Channel (channel, _) ->
                let channel = context.MessageArgs |> List.tryHead |> Option.defaultValue channel
                let historyKey = channel
                let ``to`` = utcNow()
                let from = ``to``.AddHours(-2)

                let! chatMessages =
                    ivrService.GetLines channel from ``to`` MaxLines
                    |> AsyncResult.orElseWith (fun err ->
                        match err with
                        | 404 -> AsyncResult.ok "No message(s) found"
                        | _ -> AsyncResult.error err
                    )
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                let! summary =
                    let message = [
                        {
                            Role = "user"
                            Name = None
                            Content = [
                                {
                                    Type = "text"
                                    Text = chatMessages
                                }
                            ]
                        }
                    ]

                    match chatSummaryHistory |> Dict.tryGetValue historyKey  with
                    | None ->
                        let textGenerationMessages = systemMessage :: message

                        genAIService.SendGptMessage textGenerationMessages
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")
                        |> AsyncResult.map (fun response ->
                            match response.Choices with
                            | [] -> "No response message..."
                            | choice :: _ ->
                                let message = choice.Message.Content |> stripMarkdownTags

                                chatSummaryHistory[historyKey] <- {
                                    LastMessage = utcNow()
                                    Message = message
                                }

                                message
                        )
                    | Some messages when (utcNow() - messages.LastMessage).TotalMinutes > 10 ->
                        let textGenerationMessages = systemMessage :: message

                        genAIService.SendGptMessage textGenerationMessages
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")
                        |> AsyncResult.map (fun response ->
                            match response.Choices with
                            | [] -> "No response message..."
                            | choice :: _ ->
                                let message = choice.Message.Content |> stripMarkdownTags

                                chatSummaryHistory[historyKey] <- {
                                    LastMessage = utcNow()
                                    Message = message
                                }

                                message
                        )
                    | Some messages -> async { return Ok <| messages.Message }

                if summary |> String.isEmpty then
                    return [ Message "Empty response..." ]
                else
                    return [ Message summary ]
        }
