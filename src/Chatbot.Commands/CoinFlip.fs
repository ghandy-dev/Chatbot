namespace Chatbot.Commands

[<AutoOpen>]
module CoinFlip =

    open Chatbot.Core.Domain.Commands

    let private side = [ "Heads (yes)" ; "Tails (no)" ]

    let coinFlip _ = Ok [ Message $"{side |> List.randomChoice}" ]
