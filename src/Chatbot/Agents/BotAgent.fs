module Chatbot.Agents.Bot

open System

open Microsoft.Extensions.Logging

open FsToolkit.ErrorHandling

open Chatbot.Core.Domain
open Chatbot.Core.Services.Emotes
open Chatbot.Core.IRC.Request
open Chatbot.Core.Domain.Commands
open Chatbot.Core.Domain.Commands.Parsing
open Chatbot.Core.Domain.Types
open Chatbot.Core.Types
open Chatbot.Types
open Chatbot.Twitch
open Chatbot.Common

type CommandName = string
type CooldownKey = User * CommandName
type CooldownMap = Map<CooldownKey, DateTimeOffset>

type BotMessage =
    | TwitchEvent of TwitchEvent
    | RunCommand of ValidatedCommand * Message
    | SendChannelMessage of channel: string * message: string
    | SendChannelReplyMessage of messageId: string * channel: string * message: string
    | SendWhisperMessage of fromUserId: string * toUserId: string * message: string
    | BotAction of BotAction

type State = {
    Channels: Set<string>
    UserCommandCooldowns: CooldownMap
    Emotes: Emotes
}

let create env config (users: IUsersRepository) (aliases: IAliasRepository) (emoteService: EmoteService) userId (twitchClient: TwitchClient) (triviaAgent: MailboxProcessor<Trivia.TriviaMessage>) cancellationToken =
    new MailboxProcessor<BotMessage>(
        (fun mb ->
            let logger = env.Logger

            let initial = {
                Channels = Set.empty
                UserCommandCooldowns = Map.empty
                Emotes = Emotes.empty
            }

            let sendChannelMessage channel message = twitchClient.Send (privMsg channel message)
            let sendChannelReplyMessage messageId channel message = twitchClient.Send(replyMsg messageId channel message)
            let sendWhisper fromUserId toUserId message = twitchClient.SendWhisper(fromUserId, toUserId, message)
            let joinChannel channel = twitchClient.Send (join channel)
            let partChannel channel = twitchClient.Send (part channel)

            let getOrAddUser userId username =
                async {
                    let! userOpt = users.Get (int userId)

                    match userOpt with
                    | None ->
                        let newUser = NewUser.create (int userId) username
                        let! _ = users.Add newUser
                        return User.create userId username
                    | Some user ->
                        return user
                }

            let isOnCooldown (userCommandCooldowns: CooldownMap) (user: User) (command: Command) =
                userCommandCooldowns
                |> Map.tryFind (user, command.Name)
                |> function
                | Some datetime ->
                    let delta = utcNow() - datetime
                    delta.TotalSeconds < command.Cooldown
                | None -> false

            let canExecute userCommandCooldowns user command =
                not (isOnCooldown userCommandCooldowns user command)
                && (not command.AdminOnly || user.IsAdmin)

            let runCommand state (command: Command) user (context: Context) =
                async {
                    if canExecute state.UserCommandCooldowns user command then
                        try
                            return! command.Invoke context config.Commands
                        with ex ->
                            do logger.LogError(ex, "Error occurred running command {command}", command)
                            return CommandError.internalError "Error running command"
                    else
                        return CommandError.commandOnCooldown command.Name
            }

            let dispatchCommandResponse response (msg: Message) =
                match response with
                | Error err ->
                    let message = err |> CommandError.toString

                    match msg.Source with
                    | Channel (channel, _) -> mb.Post (SendChannelMessage (channel, message))
                    | Whisper (_, fromUserId) ->  mb.Post (SendWhisperMessage (userId, fromUserId, message))

                    logger.LogWarning("Command response did not indicate success {message}", message)
                | Ok responses ->
                    responses |> List.iter (fun r ->
                        match r with
                        | CommandResponse.Message message ->
                            match msg.Source, msg.ParentMessageId with
                            | Channel (channel, _), Some messageId -> mb.Post (SendChannelReplyMessage (messageId, channel, message))
                            | Channel (channel, _), None -> mb.Post (SendChannelMessage (channel, message))
                            | Whisper (_, fromUserId), None -> mb.Post (SendWhisperMessage (userId, fromUserId, message))
                            | _ -> ()
                        | CommandResponse.BotAction action -> mb.Post (BotAction action)
                    )

            let executeCommand state command (msg: Message) =
                async {
                    let timestamp = utcNow()
                    let! user = getOrAddUser (int msg.UserId) msg.Username

                    match command with
                    | ValidatedCommand.Command (command, args) ->
                        let context = Context.create (string msg.UserId) msg.Username args msg.Source state.Emotes msg.MessageEmotes

                        let! responseResult = runCommand state (command) user context

                        let userCommandCooldowns =
                            responseResult
                            |> Result.either
                                (fun _ -> state.UserCommandCooldowns |> Map.add (user, command.Name) timestamp)
                                (fun _ -> state.UserCommandCooldowns)

                        dispatchCommandResponse responseResult msg

                        return { state with UserCommandCooldowns = userCommandCooldowns }
                    | ValidatedCommand.Pipe commands ->
                        let folder folderState (command: (Command * string list)) =
                            async {
                                let! acc, userCommandCooldowns = folderState

                                match acc with
                                | Error err -> return Error err, userCommandCooldowns
                                | Ok (CommandResponse.BotAction _) -> return CommandError.invalidUsage "Invalid command in pipe", userCommandCooldowns
                                | Ok (CommandResponse.Message acc) ->
                                    let command, args = command
                                    let args = List.append args (acc |> String.split " " |> List.ofArray)
                                    let context = Context.create (string msg.UserId) msg.Username args msg.Source state.Emotes msg.MessageEmotes

                                    let! responseResult = runCommand state command user context

                                    let userCommandCooldowns =
                                        responseResult
                                        |> Result.either
                                            (fun _ -> state.UserCommandCooldowns |> Map.add (user, command.Name) timestamp)
                                            (fun _ -> state.UserCommandCooldowns)

                                    match responseResult with
                                    | Error err -> return Error err, userCommandCooldowns
                                    | Ok responses ->
                                        let message =
                                            responses
                                            |> List.choose (function | CommandResponse.Message m -> Some m | _ -> None)
                                            |> String.join " "

                                        return Ok (Message message), userCommandCooldowns
                            }

                        let! responseResult, userCommandCooldowns =
                            commands
                            |> List.fold folder ((Ok (Message ""), state.UserCommandCooldowns) |> Async.singleton)
                            |> Async.map (fun (responseResult, cooldowns) ->
                                responseResult |> Result.map (fun r -> [r]), cooldowns
                            )

                        dispatchCommandResponse responseResult msg

                        return { state with UserCommandCooldowns = userCommandCooldowns }
                }

            let parseAndValidateCommand message =
                async {
                    return
                        parse (char config.PipeSeparator) message
                        |> validateCommand config.Commands
                }

            let tryGetAlias (message: string) userId =
                async {
                    match message |> String.split " " |> List.ofArray with
                    | [] -> return None
                    | alias :: args ->
                        match! aliases.Get (int userId) alias with
                        | None -> return None
                        | Some command ->
                            let formattedCommand = String.format command.Command args
                            return Some formattedCommand
                }

            let handleMessage (msg: Message) =
                async {
                    let! messageOpt =
                        async {
                            if msg.Message.StartsWith(config.Prefixes.AliasPrefix) then
                                return! tryGetAlias msg.Message[config.Prefixes.AliasPrefix |> String.length ..] msg.UserId
                            elif msg.Message.StartsWith(config.Prefixes.CommandPrefix) then
                                return Some msg.Message[config.Prefixes.CommandPrefix |> String.length ..]
                            else
                                return None
                        }

                    match messageOpt with
                    | None -> ()
                    | Some commandMessage ->
                        match! parseAndValidateCommand commandMessage with
                        | Error err -> do logger.LogInformation("Failed to parse/validate command {err}", err)
                        | Ok validatedCommand -> do mb.Post (RunCommand (validatedCommand, msg))
                }

            let processTwitchEvent message =
                async {
                    match message with
                    | ChannelMessage msg ->
                        let message = Message.create msg.UserId msg.Username msg.Message (MessageSource.Channel (msg.Channel, msg.ChannelId)) None msg.MessageEmotes
                        do! handleMessage message
                    | ChannelReplyMessage msg ->
                        let message = Message.create msg.UserId msg.Username $"{msg.Message} {msg.ParentMessage}" (MessageSource.Channel (msg.Channel, msg.ChannelId)) (Some msg.ParentMessageId) msg.MessageEmotes
                        do! handleMessage message
                    | WhisperMessage msg ->
                        let message = Message.create msg.UserId msg.Username msg.Message (Whisper (msg.Username, msg.UserId)) None msg.MessageEmotes
                        do! handleMessage message
                    | GlobalEmotesUpdated _ ->
                        mb.Post (BotAction (RefreshGlobalEmotes EmoteProvider.Twitch))
                    | _ -> ()
                }

            let processBotAction (state: State) action =
                async {
                    match action with
                    | JoinChannel (channel, channelId) ->
                        let! emotes = emoteService.GetChannelEmotes state.Emotes channelId
                        do joinChannel channel
                        return { state with Emotes = emotes }
                    | LeaveChannel channel ->
                        do partChannel channel
                        return state
                    | RefreshGlobalEmotes provider ->
                        let! emotes = emoteService.GetGlobalEmotes state.Emotes
                        return { state with Emotes = emotes }
                    | RefreshChannelEmotes channelId ->
                        let! emotes = emoteService.GetChannelEmotes state.Emotes channelId
                        return { state with Emotes = emotes }
                    | BotAction.StartTrivia trivia ->
                        do triviaAgent.Post (Trivia.TriviaMessage.StartTrivia trivia)
                        return state
                    | BotAction.StopTrivia channel ->
                        do triviaAgent.Post (Trivia.TriviaMessage.StopTrivia channel)
                        return state
                }

            let rec loop (state: State) =
                async {
                    match! mb.Receive() with
                    | TwitchEvent m ->
                        do! processTwitchEvent m
                    | SendChannelMessage (channel, message) ->
                        do sendChannelMessage channel message
                    | SendChannelReplyMessage (messageId, channel, message) ->
                        do sendChannelReplyMessage messageId channel message
                    | SendWhisperMessage (fromUserId, toUserId, message) ->
                        do sendWhisper fromUserId toUserId message
                    | RunCommand (command, msg) ->
                        let! state' = executeCommand state command msg
                        return! loop state'
                    | BotAction action ->
                        let! state' = processBotAction state action
                        return! loop state'

                    return! loop state
                }

            loop initial
        ),
        cancellationToken
    )
