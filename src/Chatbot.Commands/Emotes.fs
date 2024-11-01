namespace Commands

[<AutoOpen>]
module Emote =

    let private getEmoteProvider =
        function
        | "twitch" -> Emotes.EmoteProvider.Twitch
        | "bttv" -> Emotes.EmoteProvider.Bttv
        | "ffz" -> Emotes.EmoteProvider.Ffz
        | "7tv" -> Emotes.EmoteProvider.SevenTv
        | _ -> failwith "Unexpected emote provider, expected twitch/bttv/ffz/7tv"

    let private randomEmoteKeys = [
        "provider"
    ]

    let randomEmote args context =
        let keyValues = KeyValueParser.parse args randomEmoteKeys
        let maybeProvider = keyValues |> Map.tryFind "provider"

        let maybeEmote =
            match maybeProvider with
            | None ->
                context.Emotes.Random ()
            | Some p ->
                let provider = getEmoteProvider p
                context.Emotes.Random provider

        match maybeEmote with
        | None -> Message "Kappa"
        | Some emote -> Message (sprintf "%s" emote.Name)

    let refreshChannelEmotes args context =
        match context.Source with
        | Whisper _ -> Message "This command can only be used from a channel"
        | Channel channelState -> BotAction(RefreshChannelEmotes channelState.RoomId, "Refreshing channel emotes...")

    let refreshGlobalEmotes args =
        match args with
        | emoteProvider :: _ -> BotAction(RefreshGlobalEmotes(getEmoteProvider emoteProvider), "Refreshing global emotes...")
        | _ -> Message "Unknown emote provider (expected twitch/bttv/ffz/7tv)"
