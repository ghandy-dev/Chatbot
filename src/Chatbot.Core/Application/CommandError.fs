namespace Chatbot.Core.Domain.Commands

type CommandError =
    | InvalidArgs of reason: string
    | InvalidUsage of reason: string
    | InternalError of reason: string
    | Unauthorised
    | CommandOnCooldown of command: string
    | CommandNotFound of alias: string
    | AliasNotFound of alias: string
    | HttpError of service: string * error: CommandHttpError

and CommandHttpError =
    | BadRequest
    | NotFound
    | RateLimit
    | Forbidden
    | InternalServerError

module CommandError =

    let toString =
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
        | AliasNotFound alias -> $"You don't have the alias \"{alias}\""
        | CommandNotFound command -> $"Command \"{command}\" not found"
        | CommandOnCooldown command -> $"Command \"{command}\" is on cooldown"

    let invalidArgs reason = Error <| InvalidArgs reason
    let invalidUsage reason = Error <| InvalidUsage reason
    let internalError reason = Error <| InternalError reason
    let unauthorised () = Error Unauthorised
    let httpError service error = Error <| HttpError (service, error)
    let commandOnCooldown command = Error <| CommandOnCooldown command

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
