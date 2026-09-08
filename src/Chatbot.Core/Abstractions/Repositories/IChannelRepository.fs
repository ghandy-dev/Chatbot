namespace Chatbot.Core.Abstractions.Repositories

open Chatbot.Core.Domain
open Chatbot.Core.Domain.Types

type IChannelRepository =
    abstract member Get: ChannelId -> Async<Channel option>
    abstract member GetAll: unit -> Async<Channel seq>
    abstract member Delete: ChannelId -> Async<Result<int, exn>>
    abstract member Add: NewChannel -> Async<Result<int, exn>>
