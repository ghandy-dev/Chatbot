namespace Chatbot.Infrastructure.Services.Emotes.Types.Twitch

    type GlobalEmote = TTVSharp.Helix.GlobalEmote
    type ChannelEmote = TTVSharp.Helix.ChannelEmote
    type UserEmote = TTVSharp.Helix.UserEmote

namespace Chatbot.Infrastructure.Services.Emotes.Types.Bttv

    type UserEmotes = {
        ChannelEmotes: Emote list
        SharedEmotes: Emote list
    }

    and Emote = {
        Id: string
        Code: string
        ImageType: string
        Animated: bool
    }

namespace Chatbot.Infrastructure.Services.Emotes.Types.Ffz

    open System.Text.Json.Serialization

    type Room = { Sets: Map<int, EmoteSet> }

    and EmoteSet = { Emoticons: Emote list }

    and Emote = {
        Id: int
        Name: string
        Urls: Urls
        Animated: Urls option
    }

    and Urls = {
        [<JsonPropertyName("1")>]
        Small: string
        [<JsonPropertyName("2")>]
        Medium: string
        [<JsonPropertyName("4")>]
        Large: string
    }

    type GlobalEmotes = {
        [<JsonPropertyName("default_sets")>]
        DefaultSets: int list
        Sets: Map<int, EmoteSet>
    }

namespace Chatbot.Infrastructure.Services.Emotes.Types.SevenTv

    open System.Text.Json.Serialization

    type ChannelEmotes = {
        [<JsonPropertyName("emote_set")>]
        EmoteSet: EmoteSet
    }

    and EmoteSet = { Emotes: Emote list }

    and Emote = {
        Id: string
        Name: string
    }
