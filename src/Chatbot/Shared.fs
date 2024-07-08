module Chatbot.Shared

open Chatbot.Configuration
open Chatbot.Types

let botConfig = Bot.config

let ircConnection =
    ConnectionStrings.config.Irc.Split(":")
    |> function
        | [| host ; port |] -> (host, port)
        | _ -> failwith "Connection string not formatted correctly."

let mutable roomStates: RoomStates = Map.empty
