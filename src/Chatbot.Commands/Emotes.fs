namespace Chatbot.Commands

[<AutoOpen>]
module Emote =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain
    open Chatbot.Core.Services.Ivr

    let private parseEmoteProvider =
        function
        | "twitch" -> Some EmoteProvider.Twitch
        | "bttv" -> Some EmoteProvider.Bttv
        | "ffz" -> Some EmoteProvider.Ffz
        | "7tv" -> Some EmoteProvider.SevenTv
        | _ -> None

    let whatemoteisit (ivrService: IvrService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! Error <| InvalidArgs "No emote specified"
            | emote :: _ ->
                let! emote = ivrService.GetEmoteByName emote |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                let emoteInfo =
                    match emote.ChannelName, emote.EmoteTier, emote.EmoteType, emote.EmoteSetId with
                    | Some channel, Some tier, emoteType, _ -> $"https://twitch.tv/%s{channel} %s{emoteType} Tier %s{tier} emote, %s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    | Some channel, None, emoteType, _ -> $"https://twitch.tv/%s{channel} %s{emoteType} emote, %s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    | None, None, _, Some set -> $"%s{emote.EmoteCode}, ID: %s{emote.EmoteId}, Set %s{set}, %s{emote.EmoteUrl}"
                    | _ -> $"%s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"

                return [ Message emoteInfo ]
        }

    let private randomEmoteKeys = [ "provider" ]

    let randomEmote context =
        result {
            let emote =
                let kvp = KeyValueParser.parse context.MessageArgs randomEmoteKeys
                let maybeProvider = kvp.KeyValues.TryFind "provider" |> Option.bind parseEmoteProvider

                match context.MessageSource with
                | Channel (channel, _) ->
                    context.Emotes |> Emotes.random maybeProvider (Some channel)
                    |> Option.orElseWith (fun _ -> context.Emotes |> Emotes.random None None)
                | Whisper _ ->
                    maybeProvider
                    |> Option.bind (fun p -> context.Emotes |> Emotes.random (Some p) None)
                    |> Option.orElseWith (fun _ -> context.Emotes |> Emotes.random None None)
                |> Option.map _.Name
                |> Option.defaultValue "Kappa"

            return [ Message emote ]
        }

    let refreshChannelEmotes context =
        result {
            match context.MessageSource with
            | Whisper _ ->
                return! invalidUsage "This command can only be used from a channel"
            | Channel (_, channelId) ->
                return [
                    CommandResponse.refreshChannelEmotes channelId
                    Message "Refreshing channel emotes..."
                ]
        }

    let refreshGlobalEmotes context =
        result {
            match context.MessageArgs with
            | [] ->
                return! invalidArgs "No emote provider specified"
            | provider :: _ ->
                match parseEmoteProvider provider with
                | None ->
                    return! invalidArgs "Unknown emote provider specified"
                | Some p ->
                    return [
                        CommandResponse.refreshGlobalEmotes p
                        Message "Refreshing global emotes..."
                    ]
        }
