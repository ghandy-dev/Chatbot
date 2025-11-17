module MessageHandlers

open Commands
open Commands.Handler
open Configuration
open IRC
open Shared
open Types

open System.Text.RegularExpressions

let commandPrefix = appConfig.Bot.CommandPrefix

let (|Command|_|) message =
    if message |> strStartsWith commandPrefix then
        Some message[commandPrefix.Length..]
    else
        None

let private privateMessageHandler (msg: PrivateMessage) (mb: MailboxProcessor<ClientRequest>) =
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

        match message with
        | Command command ->
            let! response = safeHandleCommand msg.UserId msg.Username (Channel channelStates[msg.Channel]) command msg.Emotes

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
        | _ -> ()
    }

let private whisperMessageHandler (msg: WhisperMessage) (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg.Message with
        | Command command ->
            let! response = safeHandleCommand msg.UserId msg.DisplayName (Whisper msg.UserId) command msg.Emotes

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
        | _ -> ()
    }

let private roomStateMessageHandler (msg: RoomStateMessage) =
    match channelStates.TryGetValue msg.RoomId with
    | false, _ ->
        let roomState =
            RoomState.create
                msg.Channel
                msg.EmoteOnly
                (msg.FollowersOnly |> Option.map (fun fo -> fo.IsOn))
                msg.R9K
                msg.RoomId
                msg.Slow
                msg.SubsOnly

        channelStates[msg.Channel] <- roomState
    | true, roomState ->
        let updatedRoomState = {
            roomState with
                EmoteOnly = msg.EmoteOnly |? roomState.EmoteOnly
                FollowersOnly = msg.FollowersOnly |> Option.map (fun fo -> fo.IsOn) |? roomState.FollowersOnly
                R9K = msg.R9K |? roomState.R9K
                Slow = msg.Slow |? roomState.Slow
                SubsOnly = msg.SubsOnly |? roomState.SubsOnly
        }

        channelStates[msg.RoomId] <- updatedRoomState

let private globalUserStateMessageHandler (_: GlobalUserStateMessage) =
    async {
        do! Services.services.EmoteService.RefreshGlobalEmotes ()
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

let handleIrcMessage msg (mb: MailboxProcessor<ClientRequest>) =
    async {
        match msg with
        | PrivateMessage msg -> do! privateMessageHandler msg mb
        | WhisperMessage msg -> do! whisperMessageHandler msg mb
        | RoomStateMessage msg -> roomStateMessageHandler msg
        | UserNoticeMessage msg -> ()
        | GlobalUserStateMessage msg -> do! globalUserStateMessageHandler msg
        | UserStateMessage msg -> userStateMessageHandler msg
        | _ -> ()
    }
