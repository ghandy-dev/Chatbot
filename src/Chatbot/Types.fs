module Types

open Commands

type ClientRequest =
    | SendRawIrcMessage of string
    | SendPrivateMessage of channel: string * message: string
    | SendWhisperMessage of userId: string * username: string * message: string
    | SendReplyMessage of messageId: string * channel: string * message: string
    | SendPongMessage of string
    | BotCommand of BotCommand
    | Reconnect
