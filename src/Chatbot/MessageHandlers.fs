module Chatbot.MessageHandlers

open Chatbot.Commands
open Chatbot.Commands.Handler
open Chatbot.IRC.Messages
open Chatbot.Types
open Chatbot.Shared

let privateMessageHandler (msg: Types.PrivateMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg.Message.StartsWith(botConfig.CommandPrefix) with
        | true ->
            let! response = handleCommand msg.UserId msg.Username (Channel msg.Channel) msg.Message[1..]

            match response with
            | Some commandOutcome ->
                match commandOutcome with
                | BotAction(action, message) ->
                    mb.Post(BotCommand(action))

                    mb.Post(
                        SendPrivateMessage {
                            Channel = msg.Channel
                            Message = message
                        }
                    )
                | Message message ->
                    mb.Post(
                        SendPrivateMessage {
                            Channel = msg.Channel
                            Message = message
                        }
                    )
                | RunAlias _
                | Pipe _ -> failwith $"{nameof(RunAlias)} / {nameof(Pipe)} shouldn't hit here!"
            | None -> ()
        | false -> ()
    }

let whisperMessageHandler (msg: Types.WhisperMessage) (mb: MailboxProcessor<_>) =
    async {
        match msg.Message.StartsWith(botConfig.CommandPrefix) with
        | true ->
            let! response = handleCommand msg.UserId msg.DisplayName (Whisper msg.UserId) msg.Message[1..]

            match response with
            | Some commandOutcome ->
                match commandOutcome with
                | BotAction(action, message) ->
                    mb.Post(BotCommand(action))

                    mb.Post(
                        SendWhisperMessage {
                            UserId = msg.UserId
                            Username = msg.DisplayName
                            Message = message
                        }
                    )
                | Message message ->
                    mb.Post(
                        SendWhisperMessage {
                            UserId = msg.UserId
                            Username = msg.DisplayName
                            Message = message
                        }
                    )
                | RunAlias _
                | Pipe _ -> failwith $"{nameof(RunAlias)} / {nameof(Pipe)} shouldn't hit here!"
            | None -> ()
        | false -> ()
    }

let roomStateMessageHandler (roomStateMsg: Types.RoomStateMessage) =
    match roomStates.TryFind roomStateMsg.RoomId with
    | None ->
        let roomState =
            RoomState.create (
                roomStateMsg.Channel,
                roomStateMsg.EmoteOnly,
                roomStateMsg.FollowersOnly,
                roomStateMsg.R9K,
                roomStateMsg.RoomId,
                roomStateMsg.Slow,
                roomStateMsg.SubsOnly
            )

        roomStates <- roomStates.Add(roomState.RoomId, roomState)
    | Some oldRoomState ->
        let updatedRoomState = {
            oldRoomState with
                EmoteOnly = Option.defaultValue oldRoomState.EmoteOnly roomStateMsg.EmoteOnly
                FollowersOnly = Option.defaultValue oldRoomState.FollowersOnly roomStateMsg.FollowersOnly
                R9K = Option.defaultValue oldRoomState.R9K roomStateMsg.R9K
                Slow = Option.defaultValue oldRoomState.Slow roomStateMsg.Slow
                SubsOnly = Option.defaultValue oldRoomState.SubsOnly roomStateMsg.SubsOnly
        }

        roomStates <- roomStates.Add(roomStateMsg.RoomId, updatedRoomState)

let messageHandler (msg) (mb: MailboxProcessor<_>) =
    async {
        match msg with
        | PingMessage msg -> do mb.Post(SendPongMessage msg.message)
        | PrivateMessage msg -> do! privateMessageHandler msg mb
        | WhisperMessage msg -> do! whisperMessageHandler msg mb
        | ReconnectMessage -> mb.Post(Reconnect)
        | RoomStateMessage msg -> roomStateMessageHandler msg
        | UserNoticeMessage msg -> ()
        | _ -> ()
    }

let private parseMessages messages =
    messages |> IRC.Parsing.Parser.parseIrcMessage |> Array.map MessageMapping.mapIrcMessage |> Array.choose id

let handleMessages messages (mb: MailboxProcessor<ClientRequest>) =
    parseMessages messages |> Array.map (fun msg -> messageHandler msg mb) |> Async.Parallel |> Async.Ignore
