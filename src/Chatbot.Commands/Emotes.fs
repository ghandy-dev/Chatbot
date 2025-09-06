namespace Commands

[<AutoOpen>]
module Emote =

    open FsToolkit.ErrorHandling

    open EmoteProviders.Types
    open Services
    open CommandError

    let private ivrService = services.IvrService

    let private parseEmoteProvider =
        function
        | "twitch" -> Some EmoteProvider.Twitch
        | "bttv" -> Some EmoteProvider.Bttv
        | "ffz" -> Some EmoteProvider.Ffz
        | "7tv" -> Some EmoteProvider.SevenTv
        | _ -> None

    let whatemoteisit context =
        asyncResult {
            match context.Args with
            | [] -> return! Error <| InvalidArgs "No emote specified"
            | emote :: _ ->
                let! emote = ivrService.GetEmoteByName emote |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")

                return
                    match emote.ChannelName, emote.EmoteTier, emote.EmoteType, emote.EmoteSetId with
                    | Some channel, Some tier, emoteType, _ -> $"https://twitch.tv/%s{channel} %s{emoteType} Tier %s{tier} emote, %s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    | Some channel, None, emoteType, _ -> $"https://twitch.tv/%s{channel} %s{emoteType} emote, %s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    | None, None, _, Some set -> $"%s{emote.EmoteCode}, ID: %s{emote.EmoteId}, Set %s{set}, %s{emote.EmoteUrl}"
                    | _ -> $"%s{emote.EmoteCode}, ID: %s{emote.EmoteId}, %s{emote.EmoteUrl}"
                    |> Message
        }

    let private randomEmoteKeys = [ "provider" ]

    let randomEmote context =
        result {
            let kvp = KeyValueParser.parse context.Args randomEmoteKeys
            let maybeProvider = kvp.KeyValues.TryFind "provider" |> Option.bind parseEmoteProvider

            let emote =
                maybeProvider
                |> Option.bind (fun p -> context.Emotes.Random p)
                |> Option.orElseWith (fun _ -> context.Emotes.Random ())
                |> Option.map _.Name
                |> Option.defaultValue "Kappa"

            return Message emote
        }

    let refreshChannelEmotes context =
        result {
            match context.Source with
            | Whisper _ -> return! invalidUsage "This command can only be used from a channel"
            | Channel channelState -> return BotCommand.refreshChannelEmotes channelState.RoomId "Refreshing channel emotes..."
        }

    let refreshGlobalEmotes context =
        result {
            match context.Args with
            | [] -> return! invalidArgs "No emote provider specified"
            | provider :: _ ->
                match parseEmoteProvider provider with
                | None -> return! invalidArgs "Unknown emote provider specified"
                | Some p -> return BotCommand.refreshGlobalEmotes p "Refreshing global emotes..."
        }
