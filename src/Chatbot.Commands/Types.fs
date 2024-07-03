namespace Chatbot.Commands

type Source = Whisper of string | Channel of string

type Context = {
    UserId: string
    Username: string
    IsAdmin: bool
    Source: Source
} with

    static member createContext id username admin source = {
        UserId = id
        Username = username
        IsAdmin = admin
        Source = source
    }

type BotCommand =
    | JoinChannel of string
    | LeaveChannel of string

type CommandValue =
    | Message of string
    | RunAlias of string
    | Pipe of string list
    | BotAction of BotCommand * string

type CommandResult = Result<CommandValue, string>

type Parameters = string list

type CommandFunction =
    | SyncFunction of (unit -> CommandResult)
    | SyncFunctionWithArgs of (Parameters -> CommandResult)
    | SyncFunctionWithArgsAndContext of (Parameters -> Context -> CommandResult)
    | AsyncFunction of (unit -> Async<CommandResult>)
    | AsyncFunctionWithArgs of (Parameters -> Async<CommandResult>)
    | AsyncFunctionWithArgsAndContext of (Parameters -> Context -> Async<CommandResult>)

type Command = {
    Name: string
    Aliases: string list
    Description: string
    Execute: CommandFunction
    Cooldown: int
    AdminOnly: bool
} with

    static member createCommand (name, alias, description, func, cooldown, adminOnly) = {
        Name = name
        Aliases = alias
        Description = description
        Execute = func
        Cooldown = cooldown
        AdminOnly = adminOnly
    }
