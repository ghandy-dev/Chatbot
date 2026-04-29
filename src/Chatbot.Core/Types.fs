namespace Chatbot.Core.Types

open Microsoft.Extensions.Logging

open Chatbot.Core.Caching
open Chatbot.Core.Http
open Chatbot.Database.Db

type Env = {
    HttpClient: HttpClient
    Cache: MemoryCache
    Database: Database
    Logger: ILogger
}
