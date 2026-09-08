namespace Chatbot.Core.Abstractions.Repositories

type INewsFeedRepository =
    abstract member Get: category: string -> Async<string list>
    abstract member GetCategories: unit -> Async<string list>
