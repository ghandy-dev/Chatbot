module Emotes

open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
type EmoteProvider =
    | Twitch
    | Bttv
    | Ffz
    | SevenTv

[<RequireQualifiedAccess>]
type EmoteType =
    | Global
    | Channel
    | Subscription
    | Follower

    static member tryParse s =
        match s with
        | "none" -> Global
        | "bitstier" -> Global
        | "follower" -> Follower
        | "subscriptions" -> Subscription
        | "channelpoints" -> Global
        | "rewards" -> Global
        | "hypetrain" -> Global
        | "prime" -> Global
        | "turbo" -> Global
        | "smilies" -> Global
        | "globals" -> Global
        | "owl2019" -> Global
        | "twofactor" -> Global
        | "limitedtime" -> Global
        | _ -> Global

type Emote = {
    Name: string
    Url: string
    DirectUrl: string
    Provider: EmoteProvider
    Type: EmoteType
}

module Twitch =

    let globalEmotes () =
        async {
            match! Twitch.Helix.Emotes.getGlobalEmotes () with
            | None -> return []
            | Some emotes ->
                return
                    emotes
                    |> Seq.map (fun e -> {
                        Name = e.Name
                        Url = ""
                        DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{e.Id}/static/dark/{e.Scale |> Array.last}"
                        Type = EmoteType.Global
                        Provider = EmoteProvider.Twitch
                    })
                    |> List.ofSeq
        }

    let userEmotes (userId: string) (accessToken: string) =
        async {
            match! Twitch.Helix.Emotes.getUserEmotes userId accessToken with
            | None -> return []
            | Some emotes ->
                return
                    emotes
                    |> Seq.map (fun e -> {
                        Name = e.Name
                        Url = ""
                        DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{e.Id}/static/dark/{e.Scale |> Array.last}"
                        Type = EmoteType.tryParse e.EmoteType
                        Provider = EmoteProvider.Twitch
                    })
                    |> List.ofSeq
        }

    let channelEmotes (channel: string) =
        async {
            match! Twitch.Helix.Emotes.getChannelEmotes channel with
            | None -> return []
            | Some emotes ->
                return
                    emotes
                    |> Seq.map (fun e -> {
                        Name = e.Name
                        Url = ""
                        DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{e.Id}/{{format}}/dark/{e.Scale[1]}"
                        Type = EmoteType.tryParse e.EmoteType
                        Provider = EmoteProvider.Twitch
                    })
                    |> List.ofSeq
        }

module Bttv =

    type UserEmotes = {
        ChannelEmotes: BttvEmote list
        SharedEmotes: BttvEmote list
    }

    and BttvEmote = {
        Id: string
        Code: string
        ImageType: string
        Animated: bool
    }

    [<Literal>]
    let ApiUrl = "https://api.betterttv.net/3"

    let globalEmotesUrl = $"{ApiUrl}/cached/emotes/global"

    let channelEmotesUrl channelId =
        $"{ApiUrl}/cached/users/twitch/{channelId}"

    let emoteUrl emoteId =
        $"https://betterttv.com/emotes/{emoteId}"

    let directUrl emoteId =
        $"https://cdn.betterttv.net/emote/{emoteId}/3x"

    let globalEmotes () =
        async {
            match! Http.getFromJsonAsync<BttvEmote list> globalEmotesUrl |-> Result.toOption with
            | None -> return []
            | Some emotes ->
                return
                    emotes
                    |> List.map (fun e -> {
                        Name = e.Code
                        Url = emoteUrl e.Id
                        DirectUrl = directUrl e.Id
                        Type = EmoteType.Global
                        Provider = EmoteProvider.Bttv
                    })
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            match! Http.getFromJsonAsync<UserEmotes> url |-> Result.toOption with
            | None -> return []
            | Some emotes ->
                return
                    [ emotes.SharedEmotes ; emotes.ChannelEmotes ]
                    |> List.concat
                    |> List.map (fun e -> {
                        Name = e.Code
                        Url = emoteUrl e.Id
                        DirectUrl = directUrl e.Id
                        Type = EmoteType.Channel
                        Provider = EmoteProvider.Bttv
                    })
        }

module Ffz =

    type EmoteSet = { Emoticons: FfzEmote list }

    and FfzEmote = {
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

    type Room = { Sets: Map<int, EmoteSet> }

    [<Literal>]
    let ApiUrl = "https://api.frankerfacez.com"

    let globalEmotesUrl = $"{ApiUrl}/v1/set/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/v1/room/id/{channelId}"

    let emoteUrl emoteId =
        $"https://www.frankerfacez.com/emoticon/{emoteId}"

    let globalEmotes () =
        async {
            match! Http.getFromJsonAsync<GlobalEmotes> globalEmotesUrl |-> Result.toOption with
            | None -> return []
            | Some emotes ->
                return
                    emotes.DefaultSets
                    |> List.map (fun id -> emotes.Sets |> Map.tryFind id)
                    |> List.choose id
                    |> List.map (fun s -> s.Emoticons)
                    |> List.concat
                    |> List.map (fun e -> {
                        Name = e.Name
                        Url = emoteUrl e.Id
                        DirectUrl = (e.Animated |?? e.Urls).Large
                        Type = EmoteType.Global
                        Provider = EmoteProvider.Ffz
                    })
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            match! Http.getFromJsonAsync<Room> url |-> Result.toOption with
            | None -> return []
            | Some emotes ->
                return
                    emotes.Sets
                    |> Map.toList
                    |> List.map (fun (i, s) -> s)
                    |> List.map (fun s -> s.Emoticons)
                    |> List.concat
                    |> List.map (fun e -> {
                        Name = e.Name
                        Url = emoteUrl e.Id
                        DirectUrl = (e.Animated |?? e.Urls).Large
                        Type = EmoteType.Channel
                        Provider = EmoteProvider.Ffz
                    })
        }

module SevenTv =

    type ChannelEmotes = {
        [<JsonPropertyName("emote_set")>]
        EmoteSet: EmoteSet
    }

    and EmoteSet = { Emotes: Emote list }

    and Emote = {
        Id: string
        Name: string
    }

    [<Literal>]
    let ApiUrl = "https://7tv.io"

    let globalEmotesUrl = $"{ApiUrl}/v3/emote-sets/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/v3/users/twitch/{channelId}"

    let emoteUrl emoteId = $"https://7tv.app/emotes/{emoteId}"
    let directUrl emoteId = $"https://cdn.7tv.app/emote/{emoteId}/3x.webp"

    let globalEmotes () =
        async {
            match! Http.getFromJsonAsync<EmoteSet> globalEmotesUrl |-> Result.toOption with
            | None -> return []
            | Some set ->
                return
                    set.Emotes
                    |> List.map (fun e -> {
                        Name = e.Name
                        Url = emoteUrl e.Id
                        DirectUrl = directUrl e.Id
                        Type = EmoteType.Global
                        Provider = EmoteProvider.SevenTv
                    })
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            match! Http.getFromJsonAsync<ChannelEmotes> url |-> Result.toOption with
            | None -> return []
            | Some channel ->
                return
                    channel.EmoteSet.Emotes
                    |> List.map (fun e -> {
                        Name = e.Name
                        Url = emoteUrl e.Id
                        DirectUrl = directUrl e.Id
                        Type = EmoteType.Channel
                        Provider = EmoteProvider.SevenTv
                    })
        }

open System.Collections.Concurrent

type EmoteService() =

    let mutable globalEmotes = List.empty<Emote>
    let mutable channelEmotes = new ConcurrentDictionary<string, Emote list>()

    member _.GlobalEmotes = globalEmotes
    member _.ChannelEmotes = channelEmotes

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
