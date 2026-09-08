namespace Chatbot.Infrastructure.Services.Emotes.Mapping

open Chatbot.Core.Domain
open Chatbot.Infrastructure.Services.Emotes.Types

[<RequireQualifiedAccess>]
module Twitch =

    let directUrl emoteId scale = $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/static/dark/{scale |> Array.last}"

    let fromTwitchGlobalEmote (emote: Twitch.GlobalEmote) = {
        Name = emote.Name
        Url = ""
        DirectUrl = directUrl emote.Id emote.Scale
        Type = EmoteType.Global
        Provider = EmoteProvider.Twitch
    }

    let fromTwitchUserEmote (emote: Twitch.UserEmote) = {
        Name = emote.Name
        Url = ""
        DirectUrl = directUrl emote.Id emote.Scale
        Type = EmoteType.parse emote.EmoteType
        Provider = EmoteProvider.Twitch
    }

    let fromTwitchChannelEmote (emote: Twitch.ChannelEmote) = {
        Name = emote.Name
        Url = ""
        DirectUrl = directUrl emote.Id emote.Scale
        Type = EmoteType.parse emote.EmoteType
        Provider = EmoteProvider.Twitch
    }

[<RequireQualifiedAccess>]
module Bttv =

    let emoteUrl emoteId = $"https://betterttv.com/emotes/{emoteId}"
    let directUrl emoteId = $"https://cdn.betterttv.net/emote/{emoteId}/3x"

    let fromBttvGlobalEmote (emote: Bttv.Emote) = {
        Name = emote.Code
        Url = emoteUrl emote.Id
        DirectUrl = directUrl emote.Id
        Type = EmoteType.Global
        Provider = EmoteProvider.Bttv
    }

    let fromBttvChannelEmote (emote: Bttv.Emote) = {
        Name = emote.Code
        Url = emoteUrl emote.Id
        DirectUrl = directUrl emote.Id
        Type = EmoteType.Channel
        Provider = EmoteProvider.Bttv
    }

[<RequireQualifiedAccess>]
module Ffz =

    let emoteUrl emoteId = $"https://www.frankerfacez.com/emoticon/{emoteId}"

    let fromFfzGlobalEmote (emote: Ffz.Emote) = {
        Name = emote.Name
        Url = emoteUrl emote.Id
        DirectUrl = (emote.Animated |? emote.Urls).Large
        Type = EmoteType.Global
        Provider = EmoteProvider.Ffz
    }

    let fromFfzChannelEmote (emote: Ffz.Emote) = {
        Name = emote.Name
        Url = emoteUrl emote.Id
        DirectUrl = (emote.Animated |? emote.Urls).Large
        Type = EmoteType.Channel
        Provider = EmoteProvider.Ffz
    }

[<RequireQualifiedAccess>]
module SevenTv =

    let emoteUrl emoteId = $"https://7tv.app/emotes/{emoteId}"
    let directUrl emoteId = $"https://cdn.7tv.app/emote/{emoteId}/3x.webp"

    let fromSevenTvGlobalEmote (emote: SevenTv.Emote) = {
        Name = emote.Name
        Url = emoteUrl emote.Id
        DirectUrl = directUrl emote.Id
        Type = EmoteType.Global
        Provider = EmoteProvider.SevenTv
    }

    let fromSevenTvChannelEmote (emote: SevenTv.Emote) = {
        Name = emote.Name
        Url = emoteUrl emote.Id
        DirectUrl = directUrl emote.Id
        Type = EmoteType.Channel
        Provider = EmoteProvider.SevenTv
    }
