namespace Chatbot.Core.Domain

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

    static member parse s =
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

type Emotes = {
    GlobalEmotes: Emote list
    ChannelEmotes: Map<string, Emote list>
}

[<RequireQualifiedAccessAttribute>]
module Emotes =

    let empty = {
        GlobalEmotes = List.empty
        ChannelEmotes = Map.empty
    }

    let tryFind emote channelId emotes =
        emotes.GlobalEmotes
        |> List.tryFind (fun e -> e.Name = emote)
        |> Option.orElseWith (fun _ -> emotes.ChannelEmotes |> Map.tryFind channelId |> Option.bind (List.tryFind (fun e -> e.Name = emote)))

    let private providerFilter provider emotes = emotes |> List.filter (fun e -> e.Provider = provider)

    let private getChannelEmotes emotes channel = channel |> Option.bind (fun c -> emotes.ChannelEmotes |> Map.tryFind c) |> Option.defaultValue []

    let random provider channel emotes =
        match provider with
        | None ->
            match emotes.GlobalEmotes, getChannelEmotes emotes channel with
            | [], [] -> None
            | g, [] -> g |> List.tryRandomChoice
            | [], c -> c |> List.tryRandomChoice
            | g, c ->
                [ g ; c ]
                |> List.randomChoice
                |> List.tryRandomChoice
        | Some provider ->
            match emotes.GlobalEmotes |> providerFilter provider, getChannelEmotes emotes channel with
            | [], [] -> None
            | g, [] -> g |> List.tryRandomChoice
            | [], c -> c |> List.tryRandomChoice
            | g, c ->
                [ g ; c ]
                |> List.randomChoice
                |> List.tryRandomChoice
