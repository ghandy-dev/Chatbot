namespace Chatbot.Core.Abstractions.Repositories

open Chatbot.Core.Domain
open Chatbot.Core.Domain.Types

type IAliasRepository =
    abstract member Get: UserId -> aliasName: string -> Async<Alias option>
    abstract member Delete: aliasName: string -> UserId -> Async<Result<int, exn>>
    abstract member Add: NewAlias -> Async<Result<int, exn>>
    abstract member Update: UpdateAlias -> Async<Result<int, exn>>
