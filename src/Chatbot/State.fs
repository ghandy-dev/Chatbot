module State

open System
open System.Collections.Concurrent

open Commands
open Database.Types.Users

let userCommandCooldowns = new ConcurrentDictionary<(User * string), DateTime>()
let channelStates = new ConcurrentDictionary<string, RoomState>()
let emoteService = new Emotes.EmoteService()
