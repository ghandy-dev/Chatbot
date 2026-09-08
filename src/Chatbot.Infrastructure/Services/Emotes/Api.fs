namespace Chatbot.Infrastructure.Services

open FsToolkit.ErrorHandling

open Chatbot.Common
open Chatbot.Common.Http
open Chatbot.Core.Domain
open Chatbot.Infrastructure.Services
open Chatbot.Infrastructure.Services.Emotes.Types
open Chatbot.Infrastructure.Services.Emotes.Mapping

type EmoteService =
    abstract member GetGlobalEmotes: emotes: Emotes -> Async<Emotes>
    abstract member GetChannelEmotes: emotes: Emotes -> channelId: string -> Async<Emotes>

type EmoteProviderService = {
    GetGlobalEmotes: unit -> Async<Emote list>
    GetChannelEmotes: string -> Async<Emote list>
}

module TwitchEmoteService =

    let create (helixApi: TwitchService) =

        let getGlobalEmotes () =
            async {
                let! globalEmotes =
                    helixApi.Emotes.GetGlobalEmotes ()
                    |> AsyncResult.map (List.map Twitch.fromTwitchGlobalEmote)
                    |> AsyncResult.defaultValue []

                match! helixApi.Authentication.GetAccessToken() with
                | Error _ -> return globalEmotes
                | Ok { AccessToken = accessToken } ->
                    let! userEmotes =
                        helixApi.Users.GetAccessTokenUser accessToken
                        |> AsyncResult.bind (fun user ->
                            helixApi.Emotes.GetUserEmotes user.Id accessToken)
                        |> AsyncResult.map (List.map Twitch.fromTwitchUserEmote)
                        |> AsyncResult.defaultValue []

                    return List.concat [ globalEmotes ; userEmotes ]
            }

        let getChannelEmotes (channel: string) =
            async {
                let! emotes = helixApi.Emotes.GetChannelEmotes channel

                return
                    emotes
                    |> Result.map (List.map Twitch.fromTwitchChannelEmote)
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
                    |> Result.map (List.map Bttv.fromBttvGlobalEmote)
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
                            |> List.map Bttv.fromBttvChannelEmote)
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
                            |> List.map Ffz.fromFfzGlobalEmote
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
                        |> List.map Ffz.fromFfzChannelEmote
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
                        |> List.map SevenTv.fromSevenTvGlobalEmote)
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
                        |> List.map SevenTv.fromSevenTvChannelEmote)
                    |> Result.defaultValue []
            }

        {
            GetGlobalEmotes = getGlobalEmotes
            GetChannelEmotes = getChannelEmotes
        }

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
