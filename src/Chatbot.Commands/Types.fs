namespace Commands

type MessageSource =
    | Whisper of username: string
    | Channel of channel: RoomState

type Context = {
    UserId: string
    Username: string
    IsAdmin: bool
    Source: MessageSource
    Emotes: EmoteProviders.Types.Emotes
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
    | RefreshGlobalEmotes of emoteProvider: EmoteProviders.Types.EmoteProvider
    | StartTrivia of TriviaConfig
    | StopTrivia of channel: string

type CommandResult = Result<CommandOk, CommandError>

and CommandOk =
    | Message of string
    | RunAlias of command: string * args: string list
    | Pipe of commands: string list
    | BotAction of BotCommand * message: string option

and CommandError =
    | InvalidArgs of reason: string
    | InvalidUsage of reason: string
    | InternalError of reason: string
    | Unauthorised
    | HttpError of service: string * error: CommandHttpError

and CommandHttpError =
    | BadRequest
    | NotFound
    | RateLimit
    | Forbidden
    | InternalServerError

type Args = string list

type CommandFunction =
    | S of (unit -> CommandResult) // AsyncFunction
    | SA of (Args -> CommandResult) // AsyncFunctionWithArgs
    | SAC of (Args -> Context -> CommandResult) // AsyncFunctionWithArgsAndContext
    | SACM of (Args -> Context -> Map<string, Command> -> CommandResult) // AsyncFunctionWithArgsAndContextAndCommands
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

module BotCommand =

    let joinChannel channel channelId message = BotAction (JoinChannel (channel, channelId), Some message)
    let leaveChannel channel message = BotAction (LeaveChannel channel, Some message)
    let refreshChannelEmotes channelId message = BotAction (RefreshChannelEmotes channelId, Some message)
    let refreshGlobalEmotes emoteProvider message = BotAction (RefreshGlobalEmotes emoteProvider, Some message)
    let startTrivia config = BotAction (StartTrivia config, None)
    let stopTrivia channel = BotAction (StopTrivia channel, None)

module CommandError =

    let toMessage =
        function
        | InvalidArgs reason -> $"Invalid args: {reason}"
        | InvalidUsage reason -> $"Invalid use: {reason}"
        | Unauthorised -> $"You aren't authorised to execute this command"
        | InternalError reason -> $"Internal error occured: {reason}"
        | HttpError (service, BadRequest) -> $"{service} error. {nameof BadRequest}"
        | HttpError (service, NotFound) -> $"{service} error. {nameof NotFound}"
        | HttpError (service, RateLimit) -> $"{service} error. {nameof RateLimit}"
        | HttpError (service, Forbidden) -> $"{service} error. {nameof Forbidden}"
        | HttpError (service, InternalServerError) -> $"{service} error. {nameof InternalServerError}"

    let invalidArgs reason = Error <| InvalidArgs reason
    let invalidUsage reason = Error <| InvalidUsage reason
    let internalError reason = Error <| InternalError reason
    let unauthorised () = Error <| Unauthorised
    let httpError service error = Error <| HttpError (service, error)

module CommandHttpError =

    let fromHttpStatusCode category statusCode =
        let err =
            match statusCode with
            | 403 -> Forbidden
            | 404 -> NotFound
            | 429 -> RateLimit
            | sc when sc >= 400 && sc < 500 -> BadRequest
            | sc when sc >= 500 -> InternalServerError
            | sc -> failwith $"Unexpected HTTP status code: {sc}"

        HttpError (category, err)