namespace Commands

[<AutoOpen>]
module Slots =

    type private SetSource =
        | Static of string list
        | Emote of (Emotes.Emote list -> string list)

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
            "twitch", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = Emotes.EmoteProvider.Twitch && e.Type = Emotes.EmoteType.Global) |> List.map (fun e -> e.Name))
            "bttv", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = Emotes.EmoteProvider.Bttv && e.Type = Emotes.EmoteType.Channel) |> List.map (fun e -> e.Name))
            "ffz", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = Emotes.EmoteProvider.Ffz && e.Type = Emotes.EmoteType.Channel) |> List.map (fun e -> e.Name))
            "7tv", Emote (fun emotes -> emotes |> List.filter (fun e -> e.Provider = Emotes.EmoteProvider.SevenTv && e.Type = Emotes.EmoteType.Channel) |> List.map (fun e -> e.Name))
        ]
        |> Map.ofList

    let private keys = [ "set" ]

    let slots args context =
        let keyValues = KeyValueParser.parse args keys
        let args = KeyValueParser.removeKeyValues args keys

        let maybeSet =
            match keyValues |> Map.tryFind "set" with
            | Some set ->
                match sets |> Map.tryFind set with
                | Some (Static set) -> Some set
                | Some (Emote f) ->
                    match f (List.collect id [ context.Emotes.GlobalEmotes ; context.Emotes.ChannelEmotes ]) with
                    | [] -> None
                    | emotes -> Some emotes
                | None -> None
            | None ->
                match args with
                | [] -> None
                | _ -> Some args

        match maybeSet with
        | None -> Message "Unknown or empty set"
        | Some set ->
            let spin = set |> List.randomChoices 3

            if spin |> List.distinct |> List.length = 1 then
                let limit = 3
                let totalOutcomes = pown set.Length limit
                let probability = totalOutcomes / set.Length
                Message $"""[ {spin |> String.concat " "} ] You won! (1 in %d{probability})"""
            else
                Message $"""[ {spin |> String.concat " "} ]"""
