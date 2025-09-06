namespace Commands

open EmoteProviders.Types

[<AutoOpen>]
module Slots =

    type private SetSource =
        | Static of string list
        | Emote of (Emote list -> string list)

    let private sets =
        [
            "fruit",
                Static [
                    "🍇"
                    "🍉"
                    "🍈"
                    "🍊"
                    "🍋"
                    "🍋‍🟩"
                    "🍌"
                    "🍍"
                    "🥭"
                    "🍎"
                    "🍏"
                    "🍐"
                    "🍑"
                    "🍒"
                    "🍓"
                    "🫐"
                    "🥝"
                    "🍅"
                    "🫒"
                    "🥥"
                ]
            "numbers",
            Static [ for i in 1..100 -> $"%d{i}" ]
            "twitch", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = EmoteProvider.Twitch && e.Type = EmoteType.Global) |> List.map (fun e -> e.Name))
            "bttv", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = EmoteProvider.Bttv && e.Type = EmoteType.Channel) |> List.map (fun e -> e.Name))
            "ffz", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = EmoteProvider.Ffz && e.Type = EmoteType.Channel) |> List.map (fun e -> e.Name))
            "7tv", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = EmoteProvider.SevenTv && e.Type = EmoteType.Channel) |> List.map (fun e -> e.Name))
        ]
        |> Map.ofList

    let private keys = [ "set" ]

    let slots context =
        let kvp = KeyValueParser.parse context.Args keys

        let maybeSet =
            match kvp.KeyValues.TryFind "set" with
            | Some set ->
                match sets |> Map.tryFind set with
                | Some (Static set) -> Some set
                | Some (Emote f) ->
                    match f (List.collect id [ context.Emotes.GlobalEmotes ; context.Emotes.ChannelEmotes ]) with
                    | [] -> None
                    | emotes -> Some emotes
                | None -> None
            | None ->
                match context.Args with
                | [] -> None
                | _ -> Some context.Args

        match maybeSet with
        | None -> Error <| InvalidArgs "Unknown or empty set"
        | Some set ->
            let spin = set |> List.randomChoices 3

            if spin |> List.distinct |> List.length = 1 then
                let limit = 3
                let totalOutcomes = pown set.Length limit
                let probability = totalOutcomes / set.Length
                $"""[ {spin |> String.concat " "} ] You won! (1 in %d{probability})"""
            else
                $"""[ {spin |> String.concat " "} ]"""
            |> Message
            |> Ok
