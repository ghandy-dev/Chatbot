namespace Commands

[<AutoOpen>]
module RNG =

    open System

    open Parsing

    let private random = Random.Shared

    [<AutoOpen>]
    module Roll =

        let private defaultArgs = Some 1, Some 10

        let roll context =
            let a, b =
                match context.Args with
                | [] -> defaultArgs
                | a :: b :: _ -> tryParseInt a, tryParseInt b
                | n :: _ -> tryParseInt n, tryParseInt n

            match a, b with
            | Some a, Some b ->
                let min, max = if a > b then b, a else a, b
                let roll = random.Next(min, max)
                $"{roll}"
            | _ -> "Couldn't parse min/max value"
            |> Message
            |> Ok

    [<AutoOpen>]
    module Chance =

        let chance _ =
            let n = random.NextDouble() * 100.0
            Ok <| Message $"""{n.ToString("n2")}%%"""

    [<AutoOpen>]
    module CoinFlip =

        let private side = [ "Heads (yes)" ; "Tails (no)" ]

        let coinFlip _ = Ok <| Message $"{side |> List.randomChoice}"
