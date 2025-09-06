module Types

open Commands

type ClientRequest =
    | HandleIrcMessage of IRC.IrcMessage
    | SendRawIrcMessage of string
    | SendPrivateMessage of channel: string * message: string
    | SendWhisperMessage of userId: string * username: string * message: string
    | SendReplyMessage of messageId: string * channel: string * message: string
    | BotCommand of BotCommand

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
