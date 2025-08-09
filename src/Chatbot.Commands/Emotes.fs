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


    let whatemoteisit args context =
        asyncResult {
            match args with
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

    let private randomEmoteKeys = [
        "provider"
    ]

    let randomEmote args context =
        let kvp = KeyValueParser.parse args randomEmoteKeys
        let maybeProvider = kvp.KeyValues.TryFind "provider" |> Option.bind parseEmoteProvider

        let maybeEmote =
            match maybeProvider with
            | None -> context.Emotes.Random ()
            | Some p -> context.Emotes.Random p

        match maybeEmote with
        | None -> "Kappa"
        | Some emote -> $"%s{emote.Name}"
        |> Message
        |> Ok

    let refreshChannelEmotes args context =
        result {
            match context.Source with
            | Whisper _ -> return! invalidUsage "This command can only be used from a channel"
            | Channel channelState -> return BotAction(RefreshChannelEmotes channelState.RoomId, Some "Refreshing channel emotes...")
        }

    let refreshGlobalEmotes args =
        result {
            match args with
            | [] -> return! invalidArgs "No emote provider specified"
            | provider :: _ ->
                match parseEmoteProvider provider with
                | None -> return! invalidArgs "Unknown emote provider specified"
                | Some p -> return BotAction(RefreshGlobalEmotes p, Some "Refreshing global emotes...")
        }
