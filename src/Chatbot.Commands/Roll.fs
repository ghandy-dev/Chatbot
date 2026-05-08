namespace Chatbot.Commands

[<AutoOpen>]
module Roll =

    open Chatbot.Common.Parsing
    open Chatbot.Core.Domain.Commands

    let private random = System.Random.Shared

    let private defaultRange = Some 1, Some 10

    let roll (context: Context) =
        let a, b =
            match context.MessageArgs with
            | [] -> defaultRange
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