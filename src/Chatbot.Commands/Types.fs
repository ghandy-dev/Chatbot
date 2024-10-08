namespace Chatbot.Commands

type Source = Whisper of username: string | Channel of channel: string

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
    | JoinChannel of channel: string
    | LeaveChannel of channel: string

type CommandResult =
    | Message of string
    | RunAlias of command: string * args: string list
    | Pipe of string list
    | BotAction of BotCommand * string

type Parameters = string list

type CommandFunction =
    | SyncFunction of (unit -> CommandResult)
    | SyncFunctionWithArgs of (Parameters -> CommandResult)
    | SyncFunctionWithArgsAndContext of (Parameters -> Context -> CommandResult)
    | AsyncFunction of (unit -> Async<CommandResult>)
    | AsyncFunctionWithArgs of (Parameters -> Async<CommandResult>)
    | AsyncFunctionWithArgsAndContext of (Parameters -> Context -> Async<CommandResult>)

type Details = {
    Name: string
    Description: string
    ExampleUsage: string
}

type Command = {
    Name: string
    Aliases: string list
    Details: Details
    Execute: CommandFunction
    Cooldown: int
    AdminOnly: bool
} with

    static member createCommand (name, alias, description, func, cooldown, adminOnly) = {
        Name = name
        Aliases = alias
        Details = description
        Execute = func
        Cooldown = cooldown
        AdminOnly = adminOnly
    }
