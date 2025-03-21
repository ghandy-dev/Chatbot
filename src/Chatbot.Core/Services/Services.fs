module Services

open Geolocation.Azure
open Geolocation.Google
open Emotes
open Pastebin
open Weather

open System.Collections.Concurrent
open System.Collections.Generic

type IWeatherService =
    abstract member GetCurrentWeather: latitude: double -> longitude: double -> Async<Result<CurrentConditions, string>>

type IGeolocationService =
    abstract member GetReverseAddress: latitude: double -> longitude: double -> Async<Result<ReverseSearchAddressResultItem, string>>
    abstract member GetSearchAddress: address: string -> Async<Result<SearchAddressResultItem, string>>
    abstract member GetTimezone: latitude: double -> longitude: double -> timestamp: int64 -> Async<Result<Timezone, string>>

type IEmoteService =
    abstract member GlobalEmotes: Emote list with get
    abstract member ChannelEmotes: ConcurrentDictionary<string, Emote list> with get
    abstract member RefreshGlobalEmotes: unit -> Async<unit>
    abstract member RefreshGlobalEmotes: userId: string * accessToken: string -> Async<unit>
    abstract member RefreshChannelEmotes: channelId: string -> Async<unit>

type IPastebinService =
    abstract member CreatePaste: pasteName: string -> pasteCode: string -> Async<Result<string, string * System.Net.HttpStatusCode>>

type ITwitchService =
    abstract member GetChannel: userId: string -> Async<TTVSharp.Helix.Channel option>
    abstract member GetUserChatColor: userId: string -> Async<TTVSharp.Helix.UserChatColor option>
    abstract member GetClips: userId: string -> dateFrom: System.DateTime -> dateTo: System.DateTime -> Async<IReadOnlyList<TTVSharp.Helix.Clip> option>
    abstract member GetGlobalEmotes: unit -> Async<IReadOnlyList<TTVSharp.Helix.GlobalEmote> option>
    abstract member GetChannelEmotes: channelId: string -> Async<IReadOnlyList<TTVSharp.Helix.ChannelEmote> option>
    abstract member GetEmoteSet: emoteSetId: string -> Async<IReadOnlyList<TTVSharp.Helix.EmoteSet> option>
    abstract member GetEmoteSets: emoteSetIds: string seq -> Async<IReadOnlyList<TTVSharp.Helix.EmoteSet> option>
    abstract member GetUserEmotes: userId: string -> accessToken: string -> Async<IReadOnlyList<TTVSharp.Helix.UserEmote> option>
    abstract member GetStreams: first: int -> Async<IReadOnlyList<TTVSharp.Helix.Stream> option>
    abstract member GetStream: userId: string -> Async<TTVSharp.Helix.Stream option>
    abstract member GetUser: username: string -> Async<TTVSharp.Helix.User option>
    abstract member GetUsersByUsername: usernames: string seq -> Async<IReadOnlyList<TTVSharp.Helix.User> option>
    abstract member GetUsersById: userIds: string seq -> Async<IReadOnlyList<TTVSharp.Helix.User> option>
    abstract member GetAccessTokenUser: accessToken: string -> Async<TTVSharp.Helix.User option>
    abstract member GetLatestVod: userId: string -> Async<TTVSharp.Helix.Video option>
    abstract member SendWhisper: fromUserId: string -> toUserId: string -> message: string -> accessToken: string -> Async<int>

type IIvrService =
    abstract member GetEmoteByName: emote: string -> Async<Result<IVR.Emote, string>>
    abstract member GetSubAge: user: string -> channel: string -> Async<Result<IVR.SubAge, string>>
    abstract member GetChannelRandomLine: channel: string -> Async<Result<string, string>>
    abstract member GetUserRandomLine: channel: string -> user: string -> Async<Result<string, string>>

type Services = {
    EmoteService: IEmoteService
    GeolocationService: IGeolocationService
    IvrService: IIvrService
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
        member _.CreatePaste (pasteName: string) (pasteCode: string): Async<Result<string,(string * System.Net.HttpStatusCode)>> = createPaste pasteName pasteCode
    }


let private emoteService =
    let mutable channelEmotes = new ConcurrentDictionary<string, Emote list>()
    let mutable globalEmotes = List.empty<Emote>

    { new IEmoteService with
        member _.ChannelEmotes with get (): ConcurrentDictionary<string,Emote list> = channelEmotes

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
    }

let services = {
    EmoteService = emoteService
    GeolocationService = geolocationService
    IvrService = ivrService
    PastebinService = pastebinService
    TwitchService = twitchService
    WeatherService = weatherService
}