namespace Chatbot.Core.Domain

type Trivia = {
    Questions: Question list
    Count: int
    Categories: string list
    Timestamp: System.DateTimeOffset
    HintsSent: int list
    UseHints: bool
    Channel: string
}

and Question =  {
    Question: string
    Answer: string
    Categories: string array
    Hints: string list
    Category: string
}
