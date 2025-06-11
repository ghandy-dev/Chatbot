module Types

open Commands

type ClientRequest =
    | HandleIrcMessages of IRC.Messages.Types.IrcMessageType array
    | SendRawIrcMessage of string
    | SendPrivateMessage of channel: string * message: string
    | SendWhisperMessage of userId: string * username: string * message: string
    | SendReplyMessage of messageId: string * channel: string * message: string
    | SendPongMessage of string
    | BotCommand of BotCommand
    | Reconnect

type ReminderMessage =
    | CheckReminders
    | UserMessaged of channel: string * userId: int * username: string

type TriviaRequest =
    | StartTrivia of config: Commands.TriviaConfig
    | StopTrivia of channel: string
    | SendQuestion of channel: string
    | SendHint of channel: string
    | SendAnswer of channel: string
    | Update
    | UserMessaged of channel: string * userId: int * username: string * message: string
