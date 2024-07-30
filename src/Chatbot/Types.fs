module Chatbot.Types

open System
open Chatbot.Commands

type RoomState = {
    Channel: string
    EmoteOnly: bool
    FollowersOnly: bool
    R9K: bool
    RoomId: string
    Slow: int
    SubsOnly: bool
    LastMessageSent: DateTime
} with

    static member create (channel, emoteOnly, followersOnly, r9k, roomId, slow, subsOnly) = {
        Channel = channel
        EmoteOnly = emoteOnly |> Option.defaultValue false
        FollowersOnly = followersOnly |> Option.defaultValue false
        R9K = r9k |> Option.defaultValue false
        RoomId = roomId
        Slow = slow |> Option.defaultValue 0
        SubsOnly = subsOnly |> Option.defaultValue false
        LastMessageSent = DateTime.Now
    }

type RoomStates = Map<string, RoomState>

type State = {
    Channels: string list
    RoomStates: Map<string, RoomState>
    BotUser: string
    BotUserId: string
}

type ClientRequest =
    | SendRawIrcMessage of string
    | SendPrivateMessage of channel: string * message: string
    | SendWhisperMessage of userId: string * username: string * message: string
    | SendPongMessage of string
    | BotCommand of BotCommand
    | Reconnect
