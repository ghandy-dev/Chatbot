module Shared

open Database

open System
open System.Collections.Concurrent
open System.Text


let userCommandCooldowns = new ConcurrentDictionary<(Models.User * string), DateTime>()
let userStates = new ConcurrentDictionary<string, UserState>()
let channelStates = new ConcurrentDictionary<string, RoomState>()

let formatChatMessage (message: string) =
    if message.Length > 500 then
        message[0..496] + "..."
    else
        message