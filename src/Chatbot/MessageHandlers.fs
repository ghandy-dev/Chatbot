module Chatbot.MessageHandlers

open Chatbot
open Chatbot.Commands
open Chatbot.Commands.Handler
open Chatbot.IRC.Messages
open Chatbot.Types

let mutable roomStates : RoomStates = Map.empty
let commandPrefix = Configuration.Bot.config.CommandPrefix

let private privateMessageHandler (msg: Types.PrivateMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg.Message.StartsWith(commandPrefix) with
        | true ->
            let! response = safeHandleCommand msg.UserId msg.Username (Channel msg.Channel) msg.Message[1..]

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
                | Pipe _ -> ()
            | None -> ()
        | false -> ()
    }

let private whisperMessageHandler (msg: Types.WhisperMessage) (mb: MailboxProcessor<_>) =
    async {
        match msg.Message.StartsWith(commandPrefix) with
        | true ->
            let! response = safeHandleCommand msg.UserId msg.DisplayName (Whisper msg.UserId) msg.Message[1..]

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
                | Pipe _ -> ()
            | None -> ()
        | false -> ()
    }

let private roomStateMessageHandler (roomStateMsg: Types.RoomStateMessage) =
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

let private handleIrcMessage (msg) (mb: MailboxProcessor<_>) =
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

let private parseMessage message =
    message |> IRC.Parsing.Parser.parseIrcMessage |> Array.map MessageMapping.mapIrcMessage |> Array.choose id

let handleMessage message (mb: MailboxProcessor<ClientRequest>) =
    parseMessage message |> Array.map (fun msg -> handleIrcMessage msg mb) |> Async.Parallel |> Async.Ignore
