module MessageHandlers

open Commands
open Commands.Handler
open IRC.Messages
open State
open Types

let commandPrefix = Configuration.Bot.config.CommandPrefix

let private privateMessageHandler (msg: Types.PrivateMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg.Message.StartsWith(commandPrefix) with
        | true ->
            let! response = safeHandleCommand msg.UserId msg.Username (Channel channelStates[msg.Channel]) msg.Message[1..]

            match response with
            | Some commandOutcome ->
                match commandOutcome with
                | BotAction(action, message) ->
                    mb.Post(BotCommand(action))
                    mb.Post(SendPrivateMessage(msg.Channel, message))
                | Message message ->
                    mb.Post(SendPrivateMessage(msg.Channel, message))
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
                    mb.Post(BotCommand action)
                    mb.Post(SendWhisperMessage (msg.UserId, msg.DisplayName, message))
                | Message message ->
                    mb.Post(SendWhisperMessage (msg.UserId, msg.DisplayName, message))
                | RunAlias _
                | Pipe _ -> ()
            | None -> ()
        | false -> ()
    }

let private roomStateMessageHandler (roomStateMsg: Types.RoomStateMessage) =
    match channelStates.TryGetValue roomStateMsg.RoomId with
    | false, _ ->
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

        channelStates[roomStateMsg.Channel] <- roomState
    | true, roomState ->
        let updatedRoomState = {
            roomState with
                EmoteOnly = roomStateMsg.EmoteOnly |?? roomState.EmoteOnly
                FollowersOnly = roomStateMsg.FollowersOnly |?? roomState.FollowersOnly
                R9K = roomStateMsg.R9K |?? roomState.R9K
                Slow = roomStateMsg.Slow |?? roomState.Slow
                SubsOnly = roomStateMsg.SubsOnly |?? roomState.SubsOnly
        }

        channelStates[roomStateMsg.RoomId] <- updatedRoomState

let private handleIrcMessage msg (mb: MailboxProcessor<_>) =
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

let handleMessages messages (mb: MailboxProcessor<ClientRequest>) =
    messages |> Array.map (fun msg -> handleIrcMessage msg mb) |> Async.Parallel |> Async.Ignore
