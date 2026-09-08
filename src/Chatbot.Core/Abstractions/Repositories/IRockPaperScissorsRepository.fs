namespace Chatbot.Core.Abstractions.Repositories

open Chatbot.Core.Domain
open Chatbot.Core.Domain.Types

type IRockPaperScissorsRepository =
    abstract member Get: UserId -> Async<RpsStats option>
    abstract member Add: RpsStats -> Async<Result<int, exn>>
    abstract member Update: RpsStats -> Async<Result<int, exn>>
