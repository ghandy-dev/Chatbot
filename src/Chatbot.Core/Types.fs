namespace Chatbot.Core.Types

open Microsoft.Extensions.Logging

open Chatbot.Core.Caching
open Chatbot.Core.Http

type Env = {
    HttpClient: HttpClient
    Cache: MemoryCache
    Logger: ILogger
}
