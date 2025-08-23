module EmoteProviders

open System.Text.Json.Serialization

open FsToolkit.ErrorHandling

open Http

[<AutoOpen>]
module Types =

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

    module Twitch =

        type GlobalEmote = TTVSharp.Helix.GlobalEmote
        type ChannelEmote = TTVSharp.Helix.ChannelEmote
        type UserEmote = TTVSharp.Helix.UserEmote

    module Bttv =

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

    module Ffz =

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

    type Emote = {
        Name: string
        Url: string
        DirectUrl: string
        Provider: EmoteProvider
        Type: EmoteType
    }

    type Emotes = {
        GlobalEmotes: Emote list
        ChannelEmotes: Emote list
        MessageEmotes: Map<string, string>
    } with

        member this.TryFind (emote: string) =
            this.GlobalEmotes |> List.tryFind (fun e -> e.Name = emote) |> Option.orElseWith (fun _ -> this.ChannelEmotes |> List.tryFind (fun e -> e.Name = emote))

        member this.Random () =
            match this.GlobalEmotes, this.ChannelEmotes with
            | [], [] -> None
            | g, [] -> g |> List.tryRandomChoice
            | [], c -> c |> List.tryRandomChoice
            | g, c ->
                [ g ; c ]
                |> List.randomChoice
                |> List.tryRandomChoice

        member this.Random provider =
            match
                this.GlobalEmotes |> List.filter (fun e -> e.Provider = provider),
                this.ChannelEmotes |> List.filter (fun e -> e.Provider = provider)
            with
            | [], [] -> None
            | g, [] -> g |> List.tryRandomChoice
            | [], c -> c |> List.tryRandomChoice
            | g, c ->
                [ g ; c ]
                |> List.randomChoice
                |> List.tryRandomChoice

module Urls =

    module Twitch =

        let emoteUrl emoteId scale = $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/static/dark/{scale}"

    module Bttv =

        let emoteUrl emoteId = $"https://betterttv.com/emotes/{emoteId}"
        let directUrl emoteId = $"https://cdn.betterttv.net/emote/{emoteId}/3x"

    module Ffz =

        let emoteUrl emoteId = $"https://www.frankerfacez.com/emoticon/{emoteId}"

    module SevenTv =

        let emoteUrl emoteId = $"https://7tv.app/emotes/{emoteId}"
        let directUrl emoteId = $"https://cdn.7tv.app/emote/{emoteId}/3x.webp"

module Emote =

    let fromTwitchGlobalEmote (emote: Twitch.GlobalEmote) =
        { Name = emote.Name
          Url = ""
          DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/static/dark/{emote.Scale |> Array.last}"
          Type = EmoteType.Global
          Provider = EmoteProvider.Twitch }

    let fromTwitchUserEmote (emote: Twitch.UserEmote) =
        { Name = emote.Name
          Url = ""
          DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/static/dark/{emote.Scale |> Array.last}"
          Type = EmoteType.tryParse emote.EmoteType
          Provider = EmoteProvider.Twitch }

    let fromTwitchChannelEmote (emote: Twitch.ChannelEmote) =
        { Name = emote.Name
          Url = ""
          DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/static/dark/{emote.Scale |> Array.last}"
          Type = EmoteType.tryParse emote.EmoteType
          Provider = EmoteProvider.Twitch }

    let fromBttvGlobalEmote (emote: Bttv.Emote) =
        { Name = emote.Code
          Url = Urls.Bttv.emoteUrl emote.Id
          DirectUrl = Urls.Bttv.directUrl emote.Id
          Type = EmoteType.Global
          Provider = EmoteProvider.Bttv }

    let fromBttvChannelEmote (emote: Bttv.Emote) =
        { Name = emote.Code
          Url = Urls.Bttv.emoteUrl emote.Id
          DirectUrl = Urls.Bttv.directUrl emote.Id
          Type = EmoteType.Channel
          Provider = EmoteProvider.Bttv }

    let fromFfzGlobalEmote (emote: Ffz.Emote) =
        { Name = emote.Name
          Url = Urls.Ffz.emoteUrl emote.Id
          DirectUrl = (emote.Animated |? emote.Urls).Large
          Type = EmoteType.Global
          Provider = EmoteProvider.Ffz }

    let fromFfzChannelEmote (emote: Ffz.Emote) =
        { Name = emote.Name
          Url = Urls.Ffz.emoteUrl emote.Id
          DirectUrl = (emote.Animated |? emote.Urls).Large
          Type = EmoteType.Channel
          Provider = EmoteProvider.Ffz }

    let fromSevenTvGlobalEmote (emote: SevenTv.Emote) =
        { Name = emote.Name
          Url = Urls.Ffz.emoteUrl emote.Id
          DirectUrl = Urls.SevenTv.directUrl emote.Id
          Type = EmoteType.Global
          Provider = EmoteProvider.Ffz }

    let fromSevenTvChannelEmote (emote: SevenTv.Emote) =
        { Name = emote.Name
          Url = Urls.Ffz.emoteUrl emote.Id
          DirectUrl = Urls.SevenTv.directUrl emote.Id
          Type = EmoteType.Channel
          Provider = EmoteProvider.Ffz }


module Twitch =

    let globalEmotes () =
        async {
            let! emotes = Twitch.Helix.Emotes.getGlobalEmotes ()

            return
                emotes
                |> Result.map (List.map Emote.fromTwitchGlobalEmote)
                |> Result.defaultValue []
        }

    let channelEmotes (channel: string) =
        async {
            let! emotes = Twitch.Helix.Emotes.getChannelEmotes channel

            return
                emotes
                |> Result.map (List.map Emote.fromTwitchChannelEmote)
                |> Result.defaultValue []
        }

    let userEmotes (userId: string) (accessToken: string) =
        async {
            let! emotes = Twitch.Helix.Emotes.getUserEmotes userId accessToken

            return
                emotes
                |> Result.map (List.map Emote.fromTwitchUserEmote)
                |> Result.defaultValue []
        }

module Bttv =

    [<Literal>]
    let ApiUrl = "https://api.betterttv.net/3"
    let globalEmotesUrl = $"{ApiUrl}/cached/emotes/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/cached/users/twitch/{channelId}"

    let globalEmotes () =
        async {
            let request = Request.get globalEmotesUrl
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Bttv.Emote list>
                |> Result.map (List.map Emote.fromBttvGlobalEmote)
                |> Result.defaultValue []
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Bttv.UserEmotes>
                |> Result.map
                    (fun emotes ->
                        [ emotes.SharedEmotes ; emotes.ChannelEmotes ]
                        |> List.concat
                        |> List.map Emote.fromBttvChannelEmote)
                |> Result.defaultValue []
        }

module Ffz =

    [<Literal>]
    let ApiUrl = "https://api.frankerfacez.com"

    let globalEmotesUrl = $"{ApiUrl}/v1/set/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/v1/room/id/{channelId}"

    let globalEmotes () =
        async {
            let request = Request.get globalEmotesUrl
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Ffz.GlobalEmotes>
                |> Result.map (fun emotes ->
                        emotes.DefaultSets
                        |> List.map (fun id -> emotes.Sets |> Map.tryFind id)
                        |> List.choose id
                        |> List.map (fun s -> s.Emoticons)
                        |> List.concat
                        |> List.map Emote.fromFfzGlobalEmote
                    )
                |> Result.defaultValue []
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<Ffz.Room>
                |> Result.map (fun emotes->
                    emotes.Sets
                    |> Map.toList
                    |> List.map (fun (_, s) -> s.Emoticons)
                    |> List.concat
                    |> List.map Emote.fromFfzChannelEmote
                )
                |> Result.defaultValue []
        }

module SevenTv =

    [<Literal>]
    let ApiUrl = "https://7tv.io"

    let globalEmotesUrl = $"{ApiUrl}/v3/emote-sets/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/v3/users/twitch/{channelId}"

    let globalEmotes () =
        async {
            let request = Request.get globalEmotesUrl
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<SevenTv.EmoteSet>
                |> Result.map (fun set ->
                    set.Emotes
                    |> List.map Emote.fromSevenTvGlobalEmote)
                |> Result.defaultValue []
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            let request = Request.get url
            let! response = request |> Http.send Http.client

            return
                response
                |> Response.toJsonResult<SevenTv.ChannelEmotes>
                |> Result.map (fun set ->
                    set.EmoteSet.Emotes
                    |> List.map Emote.fromSevenTvChannelEmote)
                |> Result.defaultValue []
        }