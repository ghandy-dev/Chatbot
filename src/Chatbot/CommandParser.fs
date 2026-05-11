module Chatbot.Command.Parsing

open Chatbot.Common
open Chatbot.Types

type ParsedCommand =
    | Command of command: string * args: string list
    | AliasCommand of aliasName: string * args: string list
    | Pipe of (string * string list) list

let private tryParseCommand message =
    match message |> String.split " " |> List.ofArray with
    | [] -> None
    | command :: args -> Some (ParsedCommand.Command (command, args))

let private tryParseAlias message =
    match message |> String.split " " |> List.ofArray with
    | [] -> None
    | alias :: args -> Some (ParsedCommand.AliasCommand (alias, args))

let private tryParsePipe message =
    let pipeCommands = message |> String.split "|" |> List.ofArray

    let parsedCommands =
        pipeCommands
        |> List.map (fun pc ->
            match pc |> String.split " " |> List.ofArray with
            | [] -> None
            | command :: args -> Some (command, args)
        )

    Some (ParsedCommand.Pipe (parsedCommands |> List.choose id))

let rec tryParse prefixes message =
    if message |> String.startsWith prefixes.PipePrefix then
        tryParsePipe message[prefixes.PipePrefix.Length..]
    elif message |> String.startsWith prefixes.AliasPrefix then
        tryParseAlias message[prefixes.AliasPrefix.Length..]
    elif message |> String.startsWith prefixes.CommandPrefix then
        tryParseCommand message[prefixes.CommandPrefix.Length..]
    else
        None
