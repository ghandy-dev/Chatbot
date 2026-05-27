module Chatbot.Core.Domain.Commands.Parsing

open System
open System.Text

open FsToolkit.ErrorHandling

open Chatbot.Core.Domain.Commands

type ValidatedCommand =
    | Command of Command * string list
    | Pipe of (Command * string list) list

module Parser =

    type Token =
        | Word of string
        | Pipe

    type ParserState =
        | Normal
        | InQuotes
        | EscapedInQuotes

    let tokenize (pipeSeparator: char) (input: string) =
        let tokens = ResizeArray<Token>()

        let sb = StringBuilder()

        let flushWord () =
            if sb.Length > 0 then
                tokens.Add(Word(sb.ToString()))
                sb.Clear() |> ignore

        let rec loop i state =
            if i >= input.Length then
                flushWord ()
                tokens |> List.ofSeq
            else
                let c = input[i]

                match state with
                | Normal ->
                    match c with
                    | '"' ->
                        loop (i + 1) InQuotes
                    | _ when c = pipeSeparator ->
                        flushWord ()
                        tokens.Add(Pipe)
                        loop (i + 1) Normal
                    | c when Char.IsWhiteSpace(c) ->
                        flushWord ()
                        loop (i + 1) Normal
                    | _ ->
                        sb.Append(c) |> ignore
                        loop (i + 1) Normal
                | InQuotes ->
                    match c with
                    | '\\' ->
                        loop (i + 1) EscapedInQuotes
                    | '"' ->
                        loop (i + 1) Normal
                    | _ ->
                        sb.Append(c) |> ignore
                        loop (i + 1) InQuotes
                | EscapedInQuotes ->
                    match c with
                    | '"' ->
                        sb.Append(c) |> ignore
                    | _ ->
                        sb.Append('\\').Append(c) |> ignore

                    loop (i + 1) InQuotes

        loop 0 Normal

    let parseCommands pipeSeparator input =
        let folder acc token =
            match acc, token with
            | [], Pipe ->
                []
            | [], Word w ->
                [[w]]
            | current :: rest, Pipe ->
                [] :: current :: rest
            | current :: rest, Word w ->
                (w :: current) :: rest

        tokenize pipeSeparator input
        |> List.fold folder []
        |> List.rev
        |> List.map List.rev

let parse pipeSeparator message = Parser.parseCommands pipeSeparator message

let validateCommand (commands: Map<string, Command>) (command: string list list) =
    let findCommand commands command =
        match command with
        | [] -> Error "No command"
        | command :: args ->
            commands
            |> Map.tryFind command
            |> FSharpPlus.Option.toResultWith $"Command {command} not found"
            |> Result.map (fun r -> r, args)

    let validateSingle = findCommand commands

    let validatePipe cs =
        result {
            let! cmd, args = validateSingle cs

            if cmd.CanPipe then
                return cmd, args
            else
                return! Error $"Command \"{cmd.Name}\" does not support piping"
        }

    match command with
    | [ command ] ->
        validateSingle command
        |> Result.map (fun (c, args) -> ValidatedCommand.Command (c, args))
    | cmds ->
        cmds
        |> List.traverseResultM validatePipe
        |> Result.map (List.rev >> ValidatedCommand.Pipe)