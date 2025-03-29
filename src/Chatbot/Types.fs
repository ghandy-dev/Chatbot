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

type ReminderMessage =
    | CheckReminders
    | UserMessaged of channel: string * userId: int * username: string

type TriviaRequest =
    | StartTrivia of Commands.TriviaConfig
    | StopTrivia of channel: string
    | SendQuestion of trivia: Commands.TriviaConfig
    | SendHint of trivia: Commands.TriviaConfig
    | SendAnswer of trivia: Commands.TriviaConfig
    | Update
    | UserMessaged of channel: string * userId: int * username: string * message: string
