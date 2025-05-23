namespace Commands

type MessageSource =
    | Whisper of username: string
    | Channel of channel: RoomState

type Context = {
    UserId: string
    Username: string
    IsAdmin: bool
    Source: MessageSource
    Emotes: Emotes.Emotes
} with

    static member create id username admin source emotes = {
        UserId = id
        Username = username
        IsAdmin = admin
        Source = source
        Emotes = emotes
    }

type TriviaConfig = {
    Questions: Question list
    Count: int
    Categories: string list
    Timestamp: System.DateTime
    HintsSent: int list
    UseHints: bool
    Channel: string
}

and Question =  {
    Question: string
    Answer: string
    Categories: string array
    Hints: string list
    Category: string
}

type BotCommand =
    | JoinChannel of channel: string * channelId: string
    | LeaveChannel of channel: string
    | RefreshChannelEmotes of channelId: string
    | RefreshGlobalEmotes of emoteProvider: Emotes.EmoteProvider
    | StartTrivia of TriviaConfig
    | StopTrivia of channel: string

type CommandResult =
    | Message of string
    | RunAlias of command: string * args: string list
    | Pipe of string list
    | BotAction of BotCommand * string option

type Args = string list

type CommandFunction =
    | S of (unit -> CommandResult) // SyncFunction
    | SA of (Args -> CommandResult) // SyncFunctionWithArgs
    | SAC of (Args -> Context -> CommandResult) // SyncFunctionWithArgsAndContext
    | A of (unit -> Async<CommandResult>) // AsyncFunction
    | AA of (Args -> Async<CommandResult>) // AsyncFunctionWithArgs
    | AAC of (Args -> Context -> Async<CommandResult>) // AsyncFunctionWithArgsAndContext
    | AACM of (Args -> Context -> Map<string, Command> -> Async<CommandResult>) // AsyncFunctionWithArgsAndContextAndCommands

and Command = {
    Name: string
    Aliases: string list
    Details: Details
    Execute: CommandFunction
    Cooldown: int
    AdminOnly: bool
} with

    static member create (name, alias, description, func, cooldown, adminOnly) = {
        Name = name
        Aliases = alias
        Details = description
        Execute = func
        Cooldown = cooldown
        AdminOnly = adminOnly
    }

and Details = {
    Name: string
    Description: string
    ExampleUsage: string
}
