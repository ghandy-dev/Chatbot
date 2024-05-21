module Chatbot.Shared

open Chatbot.Configuration
open Chatbot.Types

let logger = Logging.createNamedLogger "Connection"
let botConfig = Bot.config
let connectionConfig = Connection.config
let commandPrefix = botConfig.CommandPrefix

let mutable roomStates: RoomStates = Map.empty
