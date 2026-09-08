namespace Chatbot

module MessageMapping =

    open System.Text.RegularExpressions

    open Chatbot.Common
    open Chatbot.Core.Domain
    open Chatbot.Infrastructure

    let private mentionUserRegex = new Regex($"^@\S+ ", RegexOptions.Compiled)

    let private emoteUrl id = $"https://static-cdn.jtvnw.net/emoticons/v2/%s{id}/static/dark/3.0"

    let (|ChannelMessage|_|) = function
        | IRC.Messages.PrivateMessage message ->
            match message.ReplyParentMessageId, message.ReplyParentMessageBody with
            | None, None ->
                Some
                    {
                        UserId = message.UserId
                        Username = message.Username
                        Channel = message.Channel
                        ChannelId = message.RoomId
                        Message = message.Message |> cleanInput
                        MessageEmotes = message.Emotes |> Map.map (fun _ id -> emoteUrl id)
                    } : ChannelMessage option
            | Some _, Some _ -> None
            | _ -> failwith "Expected both parent message id and parent message body, or neither"
        | _ -> None

    let (|ChannelReplyMessage|_|) = function
        | IRC.Messages.PrivateMessage message ->
            match message.ReplyParentMessageId, message.ReplyParentMessageBody with
            | None, None -> None
            | Some parentMessageId, Some parentMessage ->
                Some
                    {
                        ParentMessageId = parentMessageId
                        ParentMessage = parentMessage |> cleanInput
                        Username = message.Username
                        UserId = message.UserId
                        Channel = message.Channel
                        ChannelId = message.RoomId
                        Message = message.Message |> cleanInput |> fun text -> mentionUserRegex.Replace(text, "", 1)
                        MessageEmotes = message.Emotes |> Map.map (fun _ id -> emoteUrl id)
                    } : ChannelReplyMessage option
            | _ -> failwith "Expected both parent message id and parent message body, or neither"
        | _ -> None

    let (|WhisperMessage|_|) = function
        | IRC.Messages.WhisperMessage message ->
            Some
                {
                    UserId = message.UserId
                    Username = message.FromUser
                    Message = message.Message |> cleanInput
                    MessageEmotes = message.Emotes |> Map.map (fun _ id -> emoteUrl id)
                } : WhisperMessage option
        | _ -> None

    let (|GlobalEmotesUpdated|_|) = function
        | IRC.Messages.GlobalUserStateMessage message ->
            Some
                {
                    EmoteSets = message.EmoteSets
                } : GlobalEmotesUpdated option
        | _ -> None

    let (|RoomStateMessage|_|) = function
        | IRC.Messages.RoomStateMessage message ->
            Some
                {
                    EmoteOnly = message.EmoteOnly |> Option.defaultValue false
                    FollowersOnly = message.FollowersOnly |> Option.map FollowMode.parse |> Option.defaultValue FollowMode.Off
                    R9K = message.R9K |> Option.defaultValue false
                    ChannelId = message.RoomId
                    SlowMode = message.Slow |> Option.map SlowMmode.parse |> Option.defaultValue SlowMmode.Off
                    SubsOnly = message.SubsOnly |> Option.defaultValue false
                } : RoomStateMessage option
        | _ -> None

    let tryMapMessage message =
        match message with
        | ChannelMessage m -> Some (TwitchEvent.ChannelMessage m)
        | ChannelReplyMessage m -> Some (TwitchEvent.ChannelReplyMessage m)
        | WhisperMessage m -> Some (TwitchEvent.WhisperMessage m)
        | GlobalEmotesUpdated m -> Some (TwitchEvent.GlobalEmotesUpdated m)
        | RoomStateMessage m -> Some (TwitchEvent.RoomStateMessage m)
        | _ -> None
