namespace Chatbot.Core.Domain.Commands

type CommandResult = Result<CommandResponse list, CommandError>

type CommandFunction =
    | Sync of (Context -> CommandResult)
    | Async of (Context -> Async<CommandResult>)
    | Help of (Context -> Map<string, Command> -> CommandResult)
    | Alias of (Context -> Map<string, Command> -> Async<CommandResult>)

and Command = {
    Name: string
    Aliases: string list
    Details: Details
    Function: CommandFunction
    Cooldown: int
    AdminOnly: bool
    CanPipe: bool
} with

    member this.Invoke =
        match this.Function with
        | Sync f -> fun context _ -> async { return f context }
        | Async f -> fun context _ -> f context
        | Help f -> fun context commands -> async { return f context commands }
        | Alias f -> fun context commands -> f context commands

and Details = {
    Name: string
    Description: string
    ExampleUsage: string
}

module Command =

    let create name alias description func cooldown adminOnly canPipe = {
        Name = name
        Aliases = alias
        Details = description
        Function = func
        Cooldown = cooldown
        AdminOnly = adminOnly
        CanPipe = canPipe
    }