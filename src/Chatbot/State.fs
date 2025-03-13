module State

open System
open System.Collections.Concurrent

open Database.Types.Users

let userCommandCooldowns = new ConcurrentDictionary<(User * string), DateTime>()
let userStates = new ConcurrentDictionary<string, UserState>()
let channelStates = new ConcurrentDictionary<string, RoomState>()
