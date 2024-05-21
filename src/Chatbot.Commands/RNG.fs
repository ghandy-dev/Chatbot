namespace Chatbot.Commands

[<AutoOpen>]
module RNG =

    open System
    open Utils.Parsing

    let private random = Random.Shared

    let roll args =
        let defaultArgs = (Some 0, Some 10)

        let (min, max) =
            match args with
            | [] -> defaultArgs
            | min :: max :: _ -> (tryParseInt min, tryParseInt max)
            | n :: _ -> (tryParseInt n, tryParseInt n)

        match (min, max) with
        | (None, _) -> Error "error parsing min"
        | (_, None) -> Error "error parsing max"
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

    let coinFlip () =
        let n = random.Next(1)
        Ok <| Message $"{n}"
