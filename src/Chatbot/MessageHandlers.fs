module MessageHandlers

open Commands
open Commands.Handler
open IRC.Messages
open State
open Types

let commandPrefix = Configuration.Bot.config.CommandPrefix

let private privateMessageHandler (msg: Types.PrivateMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        let message =
            match msg.ReplyParentMessageBody, msg.ReplyParentUserLogin with
            | Some message, Some username ->
                let regex = new System.Text.RegularExpressions.Regex($"@%s{username}")
                sprintf "%s %s" (regex.Replace(msg.Message, "", 1)) message
            | _, _ -> msg.Message

        match message.StartsWith(commandPrefix) with
        | true ->
            let! response = safeHandleCommand msg.UserId msg.Username (Channel channelStates[msg.Channel]) message[1..] msg.Emotes

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
            let! response = safeHandleCommand msg.UserId msg.DisplayName (Whisper msg.UserId) msg.Message[1..] msg.Emotes

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

let private roomStateMessageHandler (msg: Types.RoomStateMessage) =
    match channelStates.TryGetValue msg.RoomId with
    | false, _ ->
        let roomState =
            RoomState.create
                msg.Channel
                msg.EmoteOnly
                msg.FollowersOnly
                msg.R9K
                msg.RoomId
                msg.Slow
                msg.SubsOnly

        channelStates[msg.Channel] <- roomState
    | true, roomState ->
        let updatedRoomState = {
            roomState with
                EmoteOnly = msg.EmoteOnly |?? roomState.EmoteOnly
                FollowersOnly = msg.FollowersOnly |?? roomState.FollowersOnly
                R9K = msg.R9K |?? roomState.R9K
                Slow = msg.Slow |?? roomState.Slow
                SubsOnly = msg.SubsOnly |?? roomState.SubsOnly
        }

        channelStates[msg.RoomId] <- updatedRoomState

let private globalUserStateMessageHandler (msg: Types.GlobalUserStateMessage) =
    async {
        do! emoteService.RefreshGlobalEmotes ()
    }

let private handleIrcMessage msg (mb: MailboxProcessor<_>) =
    async {
        match msg with
        | PingMessage msg -> do mb.Post(SendPongMessage msg.message)
        | PrivateMessage msg -> do! privateMessageHandler msg mb
        | WhisperMessage msg -> do! whisperMessageHandler msg mb
        | ReconnectMessage -> mb.Post(Reconnect)
        | RoomStateMessage msg -> roomStateMessageHandler msg
        | UserNoticeMessage msg -> ()
        | GlobalUserStateMessage msg -> do! globalUserStateMessageHandler msg
        | _ -> ()
    }

let handleMessages messages (mb: MailboxProcessor<ClientRequest>) =
    messages |> Array.map (fun msg -> handleIrcMessage msg mb) |> Async.Parallel |> Async.Ignore
