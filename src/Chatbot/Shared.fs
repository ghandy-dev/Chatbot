module Shared

open Database.Types.Users

open System
open System.Collections.Concurrent
open System.Text


let userCommandCooldowns = new ConcurrentDictionary<(User * string), DateTime>()
let userStates = new ConcurrentDictionary<string, UserState>()
let channelStates = new ConcurrentDictionary<string, RoomState>()

let formatChatMessage (message: string) =
    if message.Length > 500 then
        message[0..496] + "..."
    else
        message