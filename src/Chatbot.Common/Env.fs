namespace Chatbot.Common

open Microsoft.Extensions.Logging

open Chatbot.Common.Caching
open Chatbot.Common.Http

type Env = {
    HttpClient: HttpClient
    Cache: MemoryCache
    Logger: ILogger
}
