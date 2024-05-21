namespace Chatbot.Database

type DatabaseResult<'TSuccess> =
    | Success of 'TSuccess
    | Failure of exn
