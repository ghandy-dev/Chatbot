namespace Chatbot.Core.Abstractions.Repositories

open Chatbot.Core.Domain
open Chatbot.Core.Domain.Types

type IUsersRepository =
    abstract member Get: UserId -> Async<User option>
    abstract member Add: NewUser -> Async<Result<int, exn>>
