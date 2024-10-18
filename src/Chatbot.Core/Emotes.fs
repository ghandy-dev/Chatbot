module Emotes

open FsHttp
open FsHttp.Request
open FsHttp.Response

open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
type EmoteProvider =
    | Twitch
    | Bttv
    | Ffz
    | SevenTv

type Emote = {
    Name: string
    Url: string
    DirectUrl: string
}

type Emotes = {
    Twitch: Map<string, Emote>
    Bttv: Map<string, Emote>
    Ffz: Map<string, Emote>
    SevenTv: Map<string, Emote>
} with

    static member empty = {
        Twitch = Map.empty
        Bttv = Map.empty
        Ffz = Map.empty
        SevenTv = Map.empty
    }

    member this.Count () =
        this.Twitch.Count +
        this.Bttv.Count +
        this.Ffz.Count +
        this.SevenTv.Count

    member this.Count (provider: EmoteProvider) =
        match provider with
        | EmoteProvider.Twitch -> this.Twitch.Count
        | EmoteProvider.Bttv -> this.Bttv.Count
        | EmoteProvider.Ffz -> this.Ffz.Count
        | EmoteProvider.SevenTv -> this.SevenTv.Count

    member this.Find emote : Emote option =
        Map.tryFind emote this.Twitch
        |> Option.orElseWith (fun _ -> Map.tryFind emote this.Bttv)
        |> Option.orElseWith (fun _ -> Map.tryFind emote this.Ffz)
        |> Option.orElseWith (fun _ -> Map.tryFind emote this.SevenTv)


    member this.Random () =
        [ this.Twitch.Values
          this.Bttv.Values
          this.Ffz.Values
          this.SevenTv.Values ]
        |> Seq.concat
        |> Seq.randomChoice

    member this.Random (provider: EmoteProvider) =
        match provider with
        | EmoteProvider.Twitch -> this.Twitch.Values |> Seq.randomChoice
        | EmoteProvider.Bttv -> this.Bttv.Values |> Seq.randomChoice
        | EmoteProvider.Ffz -> this.Ffz.Values |> Seq.randomChoice
        | EmoteProvider.SevenTv -> this.SevenTv.Values |> Seq.randomChoice

let private getFromJsonAsync<'T> url =
    async {
        use! response =
            http {
                GET url
                Accept MimeTypes.applicationJson
            }
            |> sendAsync

        match response |> toResult with
        | Error _ -> return None
        | Ok res -> return! res |> deserializeJsonAsync<'T> |-> Some
    }

module Twitch =

    let globalEmotes () =
        async {
            match! Twitch.Helix.Emotes.getGlobalEmotes () with
            | None -> return Map.empty
            | Some emotes ->
                return
                    emotes
                    |> Seq.map (fun e ->
                        e.Name,
                        { Name = e.Name
                          Url = ""
                          DirectUrl = e.Images.Url2x}
                    )
                    |> Map.ofSeq
        }

    let channelEmotes channelId =
        async {
            match! Twitch.Helix.Emotes.getChannelEmotes channelId with
            | None -> return Map.empty
            | Some emotes ->
                return
                    emotes
                    |> Seq.map (fun e ->
                        e.Name,
                        { Name = e.Name
                          Url = ""
                          DirectUrl = e.Images.Url2x}
                    )
                    |> Map.ofSeq
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

    let [<Literal>] ApiUrl = "https://api.betterttv.net/3"

    let globalEmotesUrl = $"{ApiUrl}/cached/emotes/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/cached/users/twitch/{channelId}"

    let emoteUrl emoteId = $"https://betterttv.com/emotes/{emoteId}"
    let directUrl emoteId = $"https://cdn.betterttv.net/emote/{emoteId}/2x"

    let globalEmotes () =
        async {
            match! getFromJsonAsync<BttvEmote list> globalEmotesUrl with
            | None -> return Map.empty
            | Some emotes ->
                return
                    emotes
                    |> List.map (fun e ->
                        e.Code,
                        { Name = e.Code
                          Url = emoteUrl e.Id
                          DirectUrl = directUrl e.Id }
                    )
                    |> Map.ofList
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            match! getFromJsonAsync<UserEmotes> url with
            | None -> return Map.empty
            | Some emotes ->
                let emotes = List.concat [ emotes.SharedEmotes ; emotes.ChannelEmotes ]

                return
                    emotes
                    |> List.map (fun e ->
                        e.Code,
                        { Name = e.Code
                          Url = emoteUrl e.Id
                          DirectUrl = directUrl e.Id }
                    )
                    |> Map.ofList
        }

module Ffz =

    type EmoteSet = {
        Emoticons: FfzEmote list
    }

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

    type Room = {
        Sets: Map<int, EmoteSet>
    }

    let [<Literal>] ApiUrl = "https://api.frankerfacez.com"

    let globalEmotesUrl = $"{ApiUrl}/v1/set/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/v1/room/id/{channelId}"

    let emoteUrl emoteId = $"https://www.frankerfacez.com/emoticon/{emoteId}"

    let globalEmotes () =
        async {
            match! getFromJsonAsync<GlobalEmotes> globalEmotesUrl with
            | None -> return Map.empty
            | Some emotes ->
                return
                    emotes.DefaultSets
                    |> List.map (fun id -> emotes.Sets |> Map.tryFind id)
                    |> List.choose id
                    |> List.map (fun s -> s.Emoticons)
                    |> List.concat
                    |> List.map (fun e ->
                        e.Name,
                        { Name = e.Name
                          Url = emoteUrl e.Id
                          DirectUrl = (e.Animated |?? e.Urls).Medium }
                    )
                    |> Map.ofList
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            match! getFromJsonAsync<Room> url with
            | None -> return Map.empty
            | Some emotes ->
                return
                    emotes.Sets
                    |> Map.toList
                    |> List.map (fun (i, s) -> s)
                    |> List.map (fun s -> s.Emoticons)
                    |> List.concat
                    |> List.map (fun e ->
                        e.Name,
                        { Name = e.Name
                          Url = emoteUrl e.Id
                          DirectUrl = (e.Animated |?? e.Urls).Medium }
                    )
                    |> Map.ofList
        }

module SevenTv =

    type ChannelEmotes = {
        [<JsonPropertyName("emote_set")>]
        EmoteSet: EmoteSet
    }

    and EmoteSet = {
        Emotes: Emote list
    }

    and Emote = {
        Id: string
        Name: string
    }

    let [<Literal>] ApiUrl = "https://7tv.io"

    let globalEmotesUrl = $"{ApiUrl}/v3/emote-sets/global"
    let channelEmotesUrl channelId = $"{ApiUrl}/v3/users/twitch/{channelId}"

    let emoteUrl emoteId = $"https://7tv.app/emotes/{emoteId}"
    let directUrl emoteId = $"https://cdn.7tv.app/emote/{emoteId}"

    let globalEmotes () =
        async {
            match! getFromJsonAsync<EmoteSet> globalEmotesUrl with
            | None -> return Map.empty
            | Some set ->
                return
                    set.Emotes
                    |> List.map (fun e ->
                        e.Name,
                        { Name = e.Name
                          Url = emoteUrl e.Id
                          DirectUrl = directUrl e.Id }
                    )
                    |> Map.ofList
        }

    let channelEmotes channelId =
        async {
            let url = channelEmotesUrl channelId

            match! getFromJsonAsync<ChannelEmotes> url with
            | None -> return Map.empty
            | Some channel ->
                return
                    channel.EmoteSet.Emotes
                    |> List.map (fun e ->
                        e.Name,
                        { Name = e.Name
                          Url = emoteUrl e.Id
                          DirectUrl = directUrl e.Id }
                    )
                    |> Map.ofList
        }

type EmoteService() =

    let mutable globalEmotes = Emotes.empty

    member _.GlobalEmotes
        with get () = globalEmotes
        and private set (value) = globalEmotes <- value

    member val ChannelEmotes = new System.Collections.Concurrent.ConcurrentDictionary<string, Emotes>() with get

    member this.RefreshGlobalEmotes () =
        async {
            let! twitchEmotes =  Twitch.globalEmotes ()
            let! bttvEmotes =  Bttv.globalEmotes ()
            let! ffzEmotes =  Ffz.globalEmotes ()
            let! sevenTvEmotes =  SevenTv.globalEmotes ()

            this.GlobalEmotes <- {
                this.GlobalEmotes with
                    Twitch = twitchEmotes
                    Bttv = bttvEmotes
                    Ffz = ffzEmotes
                    SevenTv = sevenTvEmotes
            }
        }

    member this.RefreshGlobalEmotes emoteProvider =
        async {
            match emoteProvider with
            | EmoteProvider.Twitch ->
                let! emotes =  Twitch.globalEmotes ()
                this.GlobalEmotes <- { this.GlobalEmotes with Twitch = emotes }
            | EmoteProvider.Bttv ->
                let! emotes =  Bttv.globalEmotes ()
                this.GlobalEmotes <- { this.GlobalEmotes with Bttv = emotes }
            | EmoteProvider.Ffz ->
                let! emotes =  Ffz.globalEmotes ()
                this.GlobalEmotes <- { this.GlobalEmotes with Ffz = emotes }
            | EmoteProvider.SevenTv ->
                let! emotes =  SevenTv.globalEmotes ()
                this.GlobalEmotes <- { this.GlobalEmotes with SevenTv = emotes }
        }

    member this.RefreshChannelEmotes channelId =
        async {
            let! twitchEmotes =  Twitch.channelEmotes channelId
            let! bttvEmotes =  Bttv.channelEmotes channelId
            let! ffzEmotes =  Ffz.channelEmotes channelId
            let! sevenTvEmotes =  SevenTv.channelEmotes channelId

            this.ChannelEmotes[channelId] <- {
                Twitch = twitchEmotes
                Bttv = bttvEmotes
                Ffz = ffzEmotes
                SevenTv = sevenTvEmotes
            }
        }

    member this.RefreshChannelEmotes (channelId, emoteProvider) =
        async {
            match emoteProvider with
            | EmoteProvider.Twitch ->
                let! emotes =  Twitch.channelEmotes channelId
                match this.ChannelEmotes |> ConcurrentDictionary.tryGetValue channelId with
                | Some e -> this.ChannelEmotes[channelId] <- { e with Twitch = emotes }
                | _ -> ()
            | EmoteProvider.Bttv ->
                let! emotes =  Bttv.channelEmotes channelId
                match this.ChannelEmotes |> ConcurrentDictionary.tryGetValue channelId with
                | Some e -> this.ChannelEmotes[channelId] <- { e with Bttv = emotes }
                | _ -> ()
            | EmoteProvider.Ffz ->
                let! emotes =  Ffz.channelEmotes channelId
                match this.ChannelEmotes |> ConcurrentDictionary.tryGetValue channelId with
                | Some e -> this.ChannelEmotes[channelId] <- { e with Ffz = emotes }
                | _ -> ()
            | EmoteProvider.SevenTv ->
                let! emotes =  SevenTv.channelEmotes channelId
                match this.ChannelEmotes |> ConcurrentDictionary.tryGetValue channelId with
                | Some e -> this.ChannelEmotes[channelId] <- { e with SevenTv = emotes }
                | _ -> ()
        }
