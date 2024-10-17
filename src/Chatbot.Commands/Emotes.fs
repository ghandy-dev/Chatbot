namespace Commands

[<AutoOpen>]
module Emote =

    let randomEmote args context =
        let emote = context.Emotes.Random ()
        Message $"{emote.Name}"

    let private getEmoteProvider =
        function
        | "twitch" -> Emotes.EmoteProvider.Twitch
        | "bttv" -> Emotes.EmoteProvider.Bttv
        | "ffz" -> Emotes.EmoteProvider.Ffz
        | "7tv" -> Emotes.EmoteProvider.SevenTv
        | _ -> failwith "Unexpected emote provider, expected twitch/bttv/ffz/7tv"

    let refreshChannelEmotes args context =
        match context.Source with
        | Whisper _ -> Message "This command can only be used from a channel"
        | Channel channelState ->
            match args with
            | emoteProvider :: _ -> BotAction(RefreshChannelEmotes(channelState.RoomId, getEmoteProvider emoteProvider), "Refreshing channel emotes...")
            | _ -> Message "Unknown emote provider (expected twitch/bttv/ffz/7tv)"

    let refreshGlobalEmotes args =
        match args with
        | emoteProvider :: _ -> BotAction(RefreshGlobalEmotes(getEmoteProvider emoteProvider), "Refreshing global emotes...")
        | _ -> Message "Unknown emote provider (expected twitch/bttv/ffz/7tv)"
