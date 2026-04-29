namespace Chatbot.Core.Domain

open Chatbot.Core.Domain

type MessageSource =
    | Whisper of Username * UserId
    | Channel of ChannelName * ChannelId

type Message = {
    UserId: UserId
    Username: Username
    Message: string
    Source: MessageSource
    ParentMessageId: MessageId option
    MessageEmotes: Map<string, string>
} with

    static member create userId username message source parentId emotes = {
        UserId = userId
        Username = username
        Message = message
        Source = source
        ParentMessageId = parentId
        MessageEmotes = emotes
    }

type ChannelMessage = {
    Username: Username
    UserId: UserId
    Channel: ChannelName
    ChannelId: ChannelId
    Message: string
    MessageEmotes: Map<string, string>
}

type ChannelReplyMessage = {
    ParentMessageId: string
    ParentMessage: string
    Username: string
    UserId: string
    Channel: string
    ChannelId: string
    Message: string
    MessageEmotes: Map<string, string>
}

type WhisperMessage = {
    Username: string
    UserId: string
    Message: string
    MessageEmotes: Map<string, string>
}

type GlobalEmotesUpdated = {
    EmoteSets: string list
}

type TwitchEvent =
    | ChannelMessage of ChannelMessage
    | ChannelReplyMessage of ChannelReplyMessage
    | WhisperMessage of WhisperMessage
    | GlobalEmotesUpdated of GlobalEmotesUpdated

module Messages =

    open System.Text.RegularExpressions

    open Chatbot.Core
    open Chatbot.Common.Utils

    let private mentionUserRegex = new Regex($"^@\S+", RegexOptions.Compiled)

    let private emoteUrl id = $"https://static-cdn.jtvnw.net/emoticons/v2/%s{id}/static/dark/3.0"

    let mapPrivateMessage (message: IRC.Messages.PrivateMessage) : TwitchEvent =
        match message.ReplyParentMessageId, message.ReplyParentMessageBody with
        | None, None ->
            ChannelMessage
                {
                    UserId = message.UserId
                    Username = message.Username
                    Channel = message.Channel
                    ChannelId = message.RoomId
                    Message = message.Message |> removeHiddenChars
                    MessageEmotes = message.Emotes |> Map.map (fun _ id -> emoteUrl id)
                }
        | Some parentMessageId, Some parentMessage ->
            ChannelReplyMessage
                {
                    ParentMessageId = parentMessageId
                    ParentMessage = parentMessage
                    Username = message.Username
                    UserId = message.UserId
                    Channel = message.Channel
                    ChannelId = message.RoomId
                    Message = message.Message |> removeHiddenChars |> fun text -> mentionUserRegex.Replace(text, "", 1) // remove leading @mention placed in message
                    MessageEmotes = message.Emotes |> Map.map (fun _ id -> emoteUrl id)
                }
        | _ -> failwith "Expected both parent message id and parent message body, or neither"

    let mapWhisperMessage (message: IRC.Messages.WhisperMessage) : TwitchEvent =
        WhisperMessage
            {
                UserId = message.UserId
                Username = message.FromUser
                Message = message.Message |> removeHiddenChars
                MessageEmotes = message.Emotes |> Map.map (fun _ id -> emoteUrl id)
            }

    let mapGlobalUserStateMessage (message: IRC.Messages.GlobalUserStateMessage) : TwitchEvent =
        GlobalEmotesUpdated
            {
                EmoteSets = message.EmoteSets
            }

    let tryMapMessage message =
        match message with
        | IRC.Messages.IrcMessage.PrivateMessage m -> Some (mapPrivateMessage m)
        | IRC.Messages.IrcMessage.WhisperMessage m -> Some (mapWhisperMessage m)
        | IRC.Messages.IrcMessage.GlobalUserStateMessage m -> Some (mapGlobalUserStateMessage m)
        | _ -> None
