namespace Commands

[<AutoOpen>]
module Emote =

    open Services

    let emoteService = services.EmoteService

    let private parseEmoteProvider =
        function
        | "twitch" -> Some Emotes.EmoteProvider.Twitch
        | "bttv" -> Some Emotes.EmoteProvider.Bttv
        | "ffz" -> Some Emotes.EmoteProvider.Ffz
        | "7tv" -> Some Emotes.EmoteProvider.SevenTv
        | _ -> None

    let private randomEmoteKeys = [
        "provider"
    ]

    let whatemoteisit args context =
        async {
            match args with
            | [] -> return Message "No emote specified"
            | emote :: _ ->
                match! IVR.getEmoteByName emote with
                | Error _ -> return Message "Emote not found"
                | Ok emote ->
                    match emote.ChannelName, emote.EmoteTier, emote.EmoteType, emote.EmoteSetId with
                    | Some channel, Some tier, emoteType, _ -> return Message $"https://twitch.tv/%s{channel} %s{emoteType} Tier %s{tier} emote, %s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    | Some channel, None, emoteType, _ -> return Message $"https://twitch.tv/%s{channel} %s{emoteType} emote, %s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    | None, None, _, Some set -> return Message $"%s{emote.EmoteCode}, ID: %s{emote.EmoteId}, Set %s{set}, %s{emote.EmoteUrl}"
                    | _ -> return Message $"%s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
        }

    let randomEmote args context =
        let keyValues = KeyValueParser.parse args randomEmoteKeys
        let maybeProvider = keyValues |> Map.tryFind "provider" |> Option.bind parseEmoteProvider

        let maybeEmote =
            match maybeProvider with
            | None -> context.Emotes.Random ()
            | Some p -> context.Emotes.Random p

        match maybeEmote with
        | None -> Message "Kappa"
        | Some emote -> Message $"%s{emote.Name}"

    let refreshChannelEmotes args context =
        match context.Source with
        | Whisper _ -> Message "This command can only be used from a channel"
        | Channel channelState -> BotAction(RefreshChannelEmotes channelState.RoomId, Some "Refreshing channel emotes...")

    let refreshGlobalEmotes args =
        match args with
        | [] -> Message "No emote provider specified"
        | provider :: _ ->
            match parseEmoteProvider provider with
            | None -> Message "Unknown emote provider specified"
            | Some p -> BotAction(RefreshGlobalEmotes p, Some "Refreshing global emotes...")
