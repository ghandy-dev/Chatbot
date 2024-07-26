namespace Chatbot.Commands

[<AutoOpen>]
module RNG =

    open System

    let private random = Random.Shared

    [<AutoOpen>]
    module Roll =

        let private defaultArgs = (Some 0, Some 10)

        let roll args =

            let (min, max) =
                match args with
                | [] -> defaultArgs
                | min :: max :: _ -> (Int32.tryParse min, Int32.tryParse max)
                | n :: _ -> (Int32.tryParse n, Int32.tryParse n)

            match (min, max) with
            | (None, _) -> Error "Couldn't parse min value"
            | (_, None) -> Error "Couldn't parse max value"
            | (Some min, Some max) ->

                let n =
                    if min > max then
                        random.Next(max, min)
                    else
                        random.Next(min, max)

                Ok <| Message $"{n}"

    [<AutoOpen>]
    module Percentage =

        let chance () =
            let n = random.NextDouble() * 100.0
            Ok <| Message $"""{n.ToString("n2")}%%"""

    [<AutoOpen>]
    module CoinFlip =

        let private coinFlipSide = [ "Heads (yes)" ; "Tails (no)" ]

        let coinFlip () =
            let n = random.Next(1)
            Ok <| Message $"{coinFlipSide[n]}"
