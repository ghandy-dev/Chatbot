namespace Chatbot.Commands

[<AutoOpen>]
module RNG =

    open System

    let private random = Random.Shared

    [<AutoOpen>]
    module Roll =

        let private defaultArgs = Some 1, Some 10

        let roll (args: string list) =

            let a, b =
                match args with
                | [] -> defaultArgs
                | a :: b :: _ -> Int32.tryParse a, Int32.tryParse b
                | n :: _ -> Int32.tryParse n, Int32.tryParse n

            match a, b with
            | Some a, Some b ->
                let min, max = if a > b then b, a else a, b
                let roll = random.Next(min, max)
                Message $"{roll}"
            | _ -> Message "Couldn't parse min/max value"

    [<AutoOpen>]
    module Percentage =

        let chance () =
            let n = random.NextDouble() * 100.0
            Message $"""{n.ToString("n2")}%%"""

    [<AutoOpen>]
    module CoinFlip =

        let private coinFlipSide = [ "Heads (yes)" ; "Tails (no)" ]

        let coinFlip () = Message $"{coinFlipSide |> List.randomChoice}"
