namespace Chatbot.Commands

[<AutoOpen>]
module RNG =

    open System

    open Chatbot.Common.Parsing
    open Chatbot.Core.Domain.Commands

    let private random = Random.Shared

    [<AutoOpen>]
    module Roll =

        let private defaultArgs = Some 1, Some 10

        let roll (context: Context) =
            let a, b =
                match context.MessageArgs with
                | [] -> defaultArgs
                | a :: b :: _ -> tryParseInt a, tryParseInt b
                | n :: _ -> tryParseInt n, tryParseInt n

            let message =
                match a, b with
                | Some a, Some b ->
                    let min, max = if a > b then b, a else a, b
                    let roll = random.Next(min, max)
                    $"{roll}"
                | _ -> "Couldn't parse min/max value"

            Ok [ Message message ]

    [<AutoOpen>]
    module Chance =

        let chance _ =
            let n = random.NextDouble() * 100.0
            let message = $"""{n.ToString("n2")}%%"""

            Ok [ Message message ]

    [<AutoOpen>]
    module CoinFlip =

        let private side = [ "Heads (yes)" ; "Tails (no)" ]

        let coinFlip _ = Ok [ Message $"{side |> List.randomChoice}" ]
