namespace Chatbot.Core.Services

open Chatbot.Core

module Emotes =

    open System.Text.Json.Serialization

    module Types =

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

    open Chatbot.Core.Domain
    open Types

    module Mapping =

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

        let fromTwitchGlobalEmote (emote: Twitch.GlobalEmote) = {
            Name = emote.Name
            Url = ""
            DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/static/dark/{emote.Scale |> Array.last}"
            Type = EmoteType.Global
            Provider = EmoteProvider.Twitch
        }

        let fromTwitchUserEmote (emote: Twitch.UserEmote) = {
            Name = emote.Name
            Url = ""
            DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/static/dark/{emote.Scale |> Array.last}"
            Type = EmoteType.parse emote.EmoteType
            Provider = EmoteProvider.Twitch
        }

        let fromTwitchChannelEmote (emote: Twitch.ChannelEmote) = {
            Name = emote.Name
            Url = ""
            DirectUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/static/dark/{emote.Scale |> Array.last}"
            Type = EmoteType.parse emote.EmoteType
            Provider = EmoteProvider.Twitch
        }

        let fromBttvGlobalEmote (emote: Bttv.Emote) = {
            Name = emote.Code
            Url = Urls.Bttv.emoteUrl emote.Id
            DirectUrl = Urls.Bttv.directUrl emote.Id
            Type = EmoteType.Global
            Provider = EmoteProvider.Bttv
        }

        let fromBttvChannelEmote (emote: Bttv.Emote) = {
            Name = emote.Code
            Url = Urls.Bttv.emoteUrl emote.Id
            DirectUrl = Urls.Bttv.directUrl emote.Id
            Type = EmoteType.Channel
            Provider = EmoteProvider.Bttv
        }

        let fromFfzGlobalEmote (emote: Ffz.Emote) = {
            Name = emote.Name
            Url = Urls.Ffz.emoteUrl emote.Id
            DirectUrl = (emote.Animated |? emote.Urls).Large
            Type = EmoteType.Global
            Provider = EmoteProvider.Ffz
        }

        let fromFfzChannelEmote (emote: Ffz.Emote) = {
            Name = emote.Name
            Url = Urls.Ffz.emoteUrl emote.Id
            DirectUrl = (emote.Animated |? emote.Urls).Large
            Type = EmoteType.Channel
            Provider = EmoteProvider.Ffz
        }

        let fromSevenTvGlobalEmote (emote: SevenTv.Emote) = {
            Name = emote.Name
            Url = Urls.SevenTv.emoteUrl emote.Id
            DirectUrl = Urls.SevenTv.directUrl emote.Id
            Type = EmoteType.Global
            Provider = EmoteProvider.SevenTv
        }

        let fromSevenTvChannelEmote (emote: SevenTv.Emote) = {
            Name = emote.Name
            Url = Urls.SevenTv.emoteUrl emote.Id
            DirectUrl = Urls.SevenTv.directUrl emote.Id
            Type = EmoteType.Channel
            Provider = EmoteProvider.SevenTv
        }

    type EmoteProviderService = {
        GetGlobalEmotes: unit -> Async<Emote list>
        GetChannelEmotes: string -> Async<Emote list>
    }

    open FsToolkit.ErrorHandling

    open Chatbot.Core
    open Chatbot.Core.Http
    open Chatbot.Core.Types

    module TwitchEmoteService =

        let create (helixApi: Twitch.TwitchService) =

            let getGlobalEmotes () =
                async {
                    let! globalEmotes =
                        helixApi.Emotes.GetGlobalEmotes ()
                        |> AsyncResult.map (List.map Mapping.fromTwitchGlobalEmote)
                        |> AsyncResult.defaultValue []

                    match! helixApi.Authentication.GetAccessToken() with
                    | Error _ -> return globalEmotes
                    | Ok { AccessToken = accessToken } ->
                        let! userEmotes =
                            helixApi.Users.GetAccessTokenUser accessToken
                            |> AsyncResult.bind (fun user ->
                                helixApi.Emotes.GetUserEmotes user.Id accessToken)
                            |> AsyncResult.map (List.map Mapping.fromTwitchUserEmote)
                            |> AsyncResult.defaultValue []

                        return List.concat [ globalEmotes ; userEmotes ]
                }

            let getChannelEmotes (channel: string) =
                async {
                    let! emotes = helixApi.Emotes.GetChannelEmotes channel

                    return
                        emotes
                        |> Result.map (List.map Mapping.fromTwitchChannelEmote)
                        |> Result.defaultValue []
                }

            {
                GetGlobalEmotes = getGlobalEmotes
                GetChannelEmotes = getChannelEmotes
            }

    module BttvEmoteService =

        let create env =

            let apiUrl = "https://api.betterttv.net/3"
            let globalEmotesUrl = $"{apiUrl}/cached/emotes/global"
            let channelEmotesUrl channelId = $"{apiUrl}/cached/users/twitch/{channelId}"

            let httpClient = env.HttpClient

            let getGlobalEmotes () =
                async {
                    let request = Request.get globalEmotesUrl
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Bttv.Emote list>
                        |> Result.map (List.map Mapping.fromBttvGlobalEmote)
                        |> Result.defaultValue []
                }

            let getChannelEmotes channelId =
                async {
                    let url = channelEmotesUrl channelId

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Bttv.UserEmotes>
                        |> Result.map
                            (fun emotes ->
                                [ emotes.SharedEmotes ; emotes.ChannelEmotes ]
                                |> List.concat
                                |> List.map Mapping.fromBttvChannelEmote)
                        |> Result.defaultValue []
                }

            {
                GetGlobalEmotes = getGlobalEmotes
                GetChannelEmotes = getChannelEmotes
            }

    module FfzEmoteService =

        let create env =

            let apiUrl = "https://api.frankerfacez.com/v1"
            let globalEmotesUrl = $"{apiUrl}/set/global"
            let channelEmotesUrl channelId = $"{apiUrl}/room/id/{channelId}"

            let httpClient = env.HttpClient

            let getGlobalEmotes () =
                async {
                    let request = Request.get globalEmotesUrl
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Ffz.GlobalEmotes>
                        |> Result.map (fun emotes ->
                                emotes.DefaultSets
                                |> List.map (fun id -> emotes.Sets |> Map.tryFind id)
                                |> List.choose id
                                |> List.map (fun s -> s.Emoticons)
                                |> List.concat
                                |> List.map Mapping.fromFfzGlobalEmote
                            )
                        |> Result.defaultValue []
                }

            let getChannelEmotes channelId =
                async {
                    let url = channelEmotesUrl channelId

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<Ffz.Room>
                        |> Result.map (fun emotes->
                            emotes.Sets
                            |> Map.toList
                            |> List.map (fun (_, s) -> s.Emoticons)
                            |> List.concat
                            |> List.map Mapping.fromFfzChannelEmote
                        )
                        |> Result.defaultValue []
                }

            {
                GetGlobalEmotes = getGlobalEmotes
                GetChannelEmotes = getChannelEmotes
            }

    module SevenTvService =

        let create env =

            let apiUrl = "https://7tv.io/v3"
            let globalEmotesUrl = $"{apiUrl}/emote-sets/global"
            let channelEmotesUrl channelId = $"{apiUrl}/users/twitch/{channelId}"

            let httpClient = env.HttpClient

            let getGlobalEmotes () =
                async {
                    let request = Request.get globalEmotesUrl
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<SevenTv.EmoteSet>
                        |> Result.map (fun set ->
                            set.Emotes
                            |> List.map Mapping.fromSevenTvGlobalEmote)
                        |> Result.defaultValue []
                }

            let getChannelEmotes channelId =
                async {
                    let url = channelEmotesUrl channelId

                    let request = Request.get url
                    let! response = request |> Http.send httpClient

                    return
                        response
                        |> Response.toJsonResult<SevenTv.ChannelEmotes>
                        |> Result.map (fun set ->
                            set.EmoteSet.Emotes
                            |> List.map Mapping.fromSevenTvChannelEmote)
                        |> Result.defaultValue []
                }

            {
                GetGlobalEmotes = getGlobalEmotes
                GetChannelEmotes = getChannelEmotes
            }

    type EmoteService =
        abstract member GetGlobalEmotes: emotes: Emotes -> Async<Emotes>
        abstract member GetChannelEmotes: emotes: Emotes -> channelId: string -> Async<Emotes>

    module EmoteService =

        let create (emoteProviders: EmoteProviderService seq) =

            let getGlobalEmotes emotes  =
                async {
                    let! globalEmotes =
                        emoteProviders
                        |> Seq.map _.GetGlobalEmotes()
                        |> Async.Parallel

                    return { emotes with GlobalEmotes = globalEmotes |> List.concat }
                }

            let getChannelEmotes emotes channelId =
                async {

                    let! channelEmotes =
                        emoteProviders
                        |> Seq.map (fun ep -> ep.GetChannelEmotes channelId)
                        |> Async.Parallel

                    return { emotes with ChannelEmotes = emotes.ChannelEmotes |> Map.add channelId (channelEmotes |> List.concat) }
                }


            {
                new EmoteService with
                    member _.GetGlobalEmotes emotes = getGlobalEmotes emotes
                    member _.GetChannelEmotes emotes channelId = getChannelEmotes emotes channelId
            }
