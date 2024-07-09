namespace Chatbot.Commands

[<AutoOpen>]
module RNG =

    open System
    open Utils

    let private random = Random.Shared

    let roll args =
        let defaultArgs = (Some 0, Some 10)

        let (min, max) =
            match args with
            | [] -> defaultArgs
            | min :: max :: _ -> (Int32.tryParse min, Int32.tryParse max)
            | n :: _ -> (Int32.tryParse n, Int32.tryParse n)

        match (min, max) with
        | (None, _) -> Error "Error parsing min value."
        | (_, None) -> Error "Error parsing max value."
        | (Some min, Some max) ->

            let n =
                if min > max then
                    random.Next(max, min)
                else
                    random.Next(min, max)

            Ok <| Message $"{n}"

    let percentage () =
        let n = random.NextDouble() * 100.0
        Ok <| Message $"""{n.ToString("n2")}%%"""

    let private coinFlipSide = [ "Heads (yes)" ; "Tails (no)" ]

    let coinFlip () =
        let n = random.Next(1)
        Ok <| Message $"{coinFlipSide[n]}"
