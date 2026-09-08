namespace Chatbot.Core.Domain

type MessageSource =
    | Whisper of username: string * userId: string
    | Channel of channel: string * channelId: string

type Message = {
    UserId: string
    Username: string
    Message: string
    Source: MessageSource
    ParentMessageId: string option
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
    Username: string
    UserId: string
    Channel: string
    ChannelId: string
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

type RoomStateMessage = {
    EmoteOnly: bool
    FollowersOnly: FollowMode
    R9K: bool
    ChannelId: string
    SlowMode: SlowMmode
    SubsOnly: bool
}

and FollowMode =
    | On of int
    | Off
    with

        static member parse duration =
            match duration with
            | -1 -> FollowMode.Off
            | _ -> FollowMode.On duration

and SlowMmode =
    | On of int
    | Off
    with

        static member parse duration =
            match duration with
            | -1 -> SlowMmode.Off
            | _ -> SlowMmode.On duration

type TwitchEvent =
    | ChannelMessage of ChannelMessage
    | ChannelReplyMessage of ChannelReplyMessage
    | WhisperMessage of WhisperMessage
    | GlobalEmotesUpdated of GlobalEmotesUpdated
    | RoomStateMessage of RoomStateMessage
