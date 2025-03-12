module MessageHandlers

open Commands
open Commands.Handler
open Configuration
open IRC.Messages
open State
open Types

open System.Text.RegularExpressions

let commandPrefix = appConfig.Bot.CommandPrefix

let private privateMessageHandler (msg: Types.PrivateMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        let postMessage message =
            match msg.ReplyParentMessageId with
            | None -> mb.Post(SendPrivateMessage(msg.Channel, message))
            | Some msgId -> mb.Post(SendReplyMessage(msgId, msg.Channel, message))

        let message =
            match msg.ReplyParentMessageBody with
            | Some parentMessage ->
                let usernameRegex = new Regex($"@\w+\s*")

                if msg.ReplyParentMessageId <> msg.ReplyThreadParentMessageId then
                    sprintf "%s %s" (usernameRegex.Replace(msg.Message, "", 1)) (usernameRegex.Replace(parentMessage, "", 1))
                else
                    sprintf "%s %s" (usernameRegex.Replace(msg.Message, "", 1)) parentMessage
            | _ -> msg.Message

        match message.StartsWith(commandPrefix) with
        | true ->
            let! response = safeHandleCommand msg.UserId msg.Username (Channel channelStates[msg.Channel]) message[1..] msg.Emotes

            match response with
            | Some commandOutcome ->
                match commandOutcome with
                | BotAction(action, Some message) ->
                    mb.Post(BotCommand(action))
                    postMessage message
                | BotAction(action, None) ->
                    mb.Post(BotCommand action)
                | Message message ->
                    postMessage message
                | RunAlias _
                | Pipe _ -> ()
            | None -> ()
        | false -> ()
    }

let private whisperMessageHandler (msg: Types.WhisperMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg.Message.StartsWith(commandPrefix) with
        | true ->
            let! response = safeHandleCommand msg.UserId msg.DisplayName (Whisper msg.UserId) msg.Message[1..] msg.Emotes

            match response with
            | Some commandOutcome ->
                match commandOutcome with
                | BotAction(action, Some message) ->
                    mb.Post(BotCommand action)
                    mb.Post(SendWhisperMessage (msg.UserId, msg.DisplayName, message))
                | BotAction(action, None) ->
                    mb.Post(BotCommand action)
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

let private userStateMessageHandler (msg: UserStateMessage) =
    match msg.Id with
    | None ->
        match userStates.TryGetValue msg.DisplayName with
        | false, _ -> ()
        | true, userState ->
            let updatedUserState = {
                userState with
                    Moderator = msg.Moderator
                    Subscriber = msg.Subscriber
            }

            userStates[msg.DisplayName] <- updatedUserState
    | Some _ -> ()

let private handleIrcMessage msg (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg with
        | PingMessage msg -> do mb.Post(SendPongMessage msg.message)
        | PrivateMessage msg -> do! privateMessageHandler msg mb
        | WhisperMessage msg -> do! whisperMessageHandler msg mb
        | ReconnectMessage -> mb.Post(Reconnect)
        | RoomStateMessage msg -> roomStateMessageHandler msg
        | UserNoticeMessage msg -> ()
        | GlobalUserStateMessage msg -> do! globalUserStateMessageHandler msg
        | UserStateMessage msg -> userStateMessageHandler msg
        | _ -> ()
    }

let handleMessages messages (mb: MailboxProcessor<ClientRequest>) =
    messages |> Array.map (fun msg -> handleIrcMessage msg mb) |> Async.Parallel |> Async.Ignore
