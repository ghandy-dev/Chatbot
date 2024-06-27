namespace Chatbot.Database

[<RequireQualifiedAccess>]
type DatabaseResult<'TSuccess> =
    | Success of 'TSuccess
    | Failure of exn
