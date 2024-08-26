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
        EmoteOnly = emoteOnly |?? false
        FollowersOnly = followersOnly |?? false
        R9K = r9k |?? false
        RoomId = roomId
        Slow = slow |?? 0
        SubsOnly = subsOnly |?? false
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
