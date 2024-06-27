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

type CommandOutcome =
    | Message of string
    | RunAlias of string
    | BotAction of BotCommand * string

type CommandResult = Result<CommandOutcome, string>
type Parameters = string list

type SyncFunction = unit -> CommandResult
type AsyncFunction = unit -> Async<CommandResult>
type SyncFunctionWithArgs = Parameters -> CommandResult
type SyncFunctionWithArgsAndContext = Parameters -> Context -> CommandResult
type AsyncFunctionWithArgs = Parameters -> Async<CommandResult>
type AsyncFunctionWithArgsAndContext = Parameters -> Context -> Async<CommandResult>

type CommandFunction =
    | SyncFunction of SyncFunction
    | SyncFunctionWithArgs of SyncFunctionWithArgs
    | SyncFunctionWithArgsAndContext of SyncFunctionWithArgsAndContext
    | AsyncFunction of AsyncFunction
    | AsyncFunctionWithArgs of AsyncFunctionWithArgs
    | AsyncFunctionWithArgsAndContext of AsyncFunctionWithArgsAndContext

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
