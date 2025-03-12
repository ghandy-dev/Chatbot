namespace Trivia

module Types =

    open System

    type Question = {
        Id: string
        Categories: string array
        Question: string
        Answer: string
        Hint1: string option
        Hint2: string option
        Submitter: string option
        CreatedAt: DateTime
        Category: string
    }