module Services

open System.Collections.Generic

open EmoteProviders
open Geolocation.Azure
open Geolocation.Google
open Pastebin
open Weather

type IWeatherService =
    abstract member GetCurrentWeather: latitude: double -> longitude: double -> Async<Result<CurrentConditions list, int>>

type IGeolocationService =
    abstract member GetReverseAddress: latitude: double -> longitude: double -> Async<Result<ReverseSearchAddressResultItem, int>>
    abstract member GetSearchAddress: address: string -> Async<Result<SearchAddressResultItem, int>>
    abstract member GetTimezone: latitude: double -> longitude: double -> timestamp: int64 -> Async<Result<Timezone, int>>

type IEmoteService =
    abstract member GlobalEmotes: Emote list with get
    abstract member ChannelEmotes: Dictionary<string, Emote list> with get
    abstract member RefreshGlobalEmotes: unit -> Async<unit>
    abstract member RefreshGlobalEmotes: userId: string * accessToken: string -> Async<unit>
    abstract member RefreshChannelEmotes: channelId: string -> Async<unit>

type IPastebinService =
    abstract member CreatePaste: pasteName: string -> pasteCode: string -> Async<Result<string, int>>

type ITwitchService =
    abstract member GetChannel: userId: string -> Async<Result<TTVSharp.Helix.Channel option, int>>
    abstract member GetUserChatColor: userId: string -> Async<Result<TTVSharp.Helix.UserChatColor option, int>>
    abstract member GetClips: userId: string -> dateFrom: System.DateTime -> dateTo: System.DateTime -> Async<Result<TTVSharp.Helix.Clip list, int>>
    abstract member GetGlobalEmotes: unit -> Async<Result<TTVSharp.Helix.GlobalEmote list, int>>
    abstract member GetChannelEmotes: channelId: string -> Async<Result<TTVSharp.Helix.ChannelEmote list, int>>
    abstract member GetEmoteSet: emoteSetId: string -> Async<Result<TTVSharp.Helix.EmoteSet list, int>>
    abstract member GetEmoteSets: emoteSetIds: string seq -> Async<Result<TTVSharp.Helix.EmoteSet list, int>>
    abstract member GetUserEmotes: userId: string -> accessToken: string -> Async<Result<TTVSharp.Helix.UserEmote list, int>>
    abstract member GetStreams: first: int -> Async<Result<TTVSharp.Helix.Stream list, int>>
    abstract member GetStream: userId: string -> Async<Result<TTVSharp.Helix.Stream option, int>>
    abstract member GetUser: username: string -> Async<Result<TTVSharp.Helix.User option, int>>
    abstract member GetUsersByUsername: usernames: string seq -> Async<Result<TTVSharp.Helix.User list, int>>
    abstract member GetUsersById: userIds: string seq -> Async<Result<TTVSharp.Helix.User list, int>>
    abstract member GetAccessTokenUser: accessToken: string -> Async<Result<TTVSharp.Helix.User option, int>>
    abstract member GetLatestVod: userId: string -> Async<Result<TTVSharp.Helix.Video option, int>>
    abstract member SendWhisper: fromUserId: string -> toUserId: string -> message: string -> accessToken: string -> Async<int>

type IIvrService =
    abstract member GetEmoteByName: emote: string -> Async<Result<IVR.Emote, int>>
    abstract member GetSubAge: user: string -> channel: string -> Async<Result<IVR.SubAge, int>>
    abstract member GetChannelRandomLine: channel: string -> Async<Result<string, int>>
    abstract member GetUserRandomLine: channel: string -> user: string -> Async<Result<string, int>>
    abstract member Search: channel: string -> user: string -> query: string -> reverse: bool -> offset: int -> Async<Result<string, int>>
    abstract member GetLastLine: channel: string -> user: string -> Async<Result<string, int>>
    abstract member GetLines: channel: string -> from: System.DateTime -> ``to``: System.DateTime -> limit: int -> Async<Result<string, int>>

type IOpenAIService =
    abstract member GetImage: prompt: string -> Async<Result<OpenAI.Image.GenerateImageResponse, int>>
    abstract member SendGptMessage: message: OpenAI.Chat.TextGenerationMessage list -> Async<Result<OpenAI.Chat.TextGenerationMessageResponse, int>>

type IImageUploadService =
    abstract member Upload: bytes: byte array -> Async<Result<string, int>>

type Services = {
    EmoteService: IEmoteService
    GeolocationService: IGeolocationService
    ImageUploadService: IImageUploadService
    IvrService: IIvrService
    OpenAiService: IOpenAIService
    PastebinService: IPastebinService
    TwitchService: ITwitchService
    WeatherService: IWeatherService
}

let private weatherService =
    { new IWeatherService with
        member _.GetCurrentWeather latitude longitude = getCurrentWeather latitude longitude
    }

let private geolocationService =
    { new IGeolocationService with
        member _.GetReverseAddress latitude longitude = getReverseAddress latitude longitude
        member _.GetSearchAddress address = getSearchAddress address
        member _.GetTimezone latitude longitude timestamp = getTimezone latitude longitude timestamp
    }

let private pastebinService =
    { new IPastebinService with
        member _.CreatePaste (pasteName: string) (pasteCode: string) = createPaste pasteName pasteCode
    }

let private emoteService =
    let mutable channelEmotes = new Dictionary<string, Emote list>()
    let mutable globalEmotes = List.empty<Emote>

    { new IEmoteService with
        member _.ChannelEmotes with get (): Dictionary<string,Emote list> = channelEmotes

        member _.GlobalEmotes with get (): Emote list = globalEmotes

        member _.RefreshGlobalEmotes () =
            async {
                let! twitchEmotes = Twitch.globalEmotes ()
                let! bttvEmotes = Bttv.globalEmotes ()
                let! ffzEmotes = Ffz.globalEmotes ()
                let! sevenTvEmotes = SevenTv.globalEmotes ()

                globalEmotes <- [ twitchEmotes ; bttvEmotes ; ffzEmotes ; sevenTvEmotes ] |> List.concat
            }

        member _.RefreshGlobalEmotes (userId: string, accessToken: string) =
            async {
                let! twitchEmotes = Twitch.userEmotes userId accessToken
                let! bttvEmotes = Bttv.globalEmotes ()
                let! ffzEmotes = Ffz.globalEmotes ()
                let! sevenTvEmotes = SevenTv.globalEmotes ()

                globalEmotes <- [ twitchEmotes ; bttvEmotes ; ffzEmotes ; sevenTvEmotes ] |> List.concat
            }

        member _.RefreshChannelEmotes (channelId: string) =
            async {
                let! twitchEmotes = Twitch.channelEmotes channelId
                let! bttvEmotes = Bttv.channelEmotes channelId
                let! ffzEmotes = Ffz.channelEmotes channelId
                let! sevenTvEmotes = SevenTv.channelEmotes channelId

                channelEmotes[channelId] <-
                    [
                        twitchEmotes |> List.filter (fun e -> e.Type = EmoteType.Follower)
                        bttvEmotes
                        ffzEmotes
                        sevenTvEmotes
                    ]
                    |> List.concat
            }
    }

let private twitchService =
    { new ITwitchService with
        member _.GetAccessTokenUser accessToken = Twitch.Helix.Users.getAccessTokenUser accessToken
        member _.GetChannel userId = Twitch.Helix.Channels.getChannel userId
        member _.GetChannelEmotes channelId = Twitch.Helix.Emotes.getChannelEmotes channelId
        member _.GetClips userId dateFrom dateTo = Twitch.Helix.Clips.getClips userId dateFrom dateTo
        member _.GetEmoteSet emoteSetId = Twitch.Helix.Emotes.getEmoteSet emoteSetId
        member _.GetEmoteSets emoteSetIds = Twitch.Helix.Emotes.getEmoteSets emoteSetIds
        member _.GetGlobalEmotes() = Twitch.Helix.Emotes.getGlobalEmotes ()
        member _.GetLatestVod userId = Twitch.Helix.Videos.getLatestVod userId
        member _.GetStream (userId: string) = Twitch.Helix.Streams.getStream userId
        member _.GetStreams first = Twitch.Helix.Streams.getStreams first
        member _.GetUser username = Twitch.Helix.Users.getUser username
        member _.GetUserChatColor userId = Twitch.Helix.Chat.getUserChatColor userId
        member _.GetUserEmotes userId (accessToken: string) = Twitch.Helix.Emotes.getUserEmotes userId accessToken
        member _.GetUsersById userIds = Twitch.Helix.Users.getUsersById userIds
        member _.GetUsersByUsername usernames = Twitch.Helix.Users.getUsersByUsername usernames
        member _.SendWhisper fromUserId toUserId message accessToken = Twitch.Helix.Whispers.sendWhisper fromUserId toUserId message accessToken
    }

let ivrService =
    { new IIvrService with
        member _.GetChannelRandomLine channel = IVR.getChannelRandomLine channel
        member _.GetEmoteByName emote = IVR.getEmoteByName emote
        member _.GetSubAge user channel = IVR.getSubAge user channel
        member _.GetUserRandomLine channel user = IVR.getUserRandomLine channel user
        member _.Search channel user query reverse offset = IVR.search channel user query reverse offset
        member _.GetLastLine channel user = IVR.getLastLine channel user
        member _.GetLines channel from ``to`` limit = IVR.getLines channel from ``to`` limit
    }

let openAiService =
    { new IOpenAIService with
        member _.GetImage(prompt: string) = OpenAI.getImage prompt
        member _.SendGptMessage(messages: OpenAI.Chat.TextGenerationMessage list) = OpenAI.sendGptMessage messages
    }

let imageUploadService =
    { new IImageUploadService with
        member _.Upload(bytes: byte array) = ImageUploader.upload bytes
    }

let services = {
    EmoteService = emoteService
    GeolocationService = geolocationService
    ImageUploadService = imageUploadService
    IvrService = ivrService
    OpenAiService = openAiService
    PastebinService = pastebinService
    TwitchService = twitchService
    WeatherService = weatherService
}