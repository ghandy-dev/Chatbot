module Chatbot.Commands.Handler

open Chatbot
open Chatbot.Commands
open Chatbot.Database
open Chatbot.Database.Types

open System

let private users =
    new Collections.Concurrent.ConcurrentDictionary<(User * string), DateTime>()

let private applyFunction =
    function
    | SyncFunction f -> fun _ _ -> async { return f () }
    | SyncFunctionWithArgs f -> fun args _ -> async { return f args }
    | SyncFunctionWithArgsAndContext f -> fun args ctx -> async { return f args ctx }
    | AsyncFunction f -> fun _ _ -> f ()
    | AsyncFunctionWithArgs f -> fun args _ -> f args
    | AsyncFunctionWithArgsAndContext f -> fun args ctx -> f args ctx

let private getUser userId username =
    async {
        match! UserRepository.getById (userId |> int) with
        | None ->
            match! UserRepository.add (User.create (userId |> int) username) with
            | DatabaseResult.Failure -> return failwith "Couldn't add user to database."
            | DatabaseResult.Success _ ->
                match! UserRepository.getById (userId |> int) with
                | None -> return failwith "Couldn't retrieve new user"
                | Some user -> return user
        | Some user -> return user
    }

let private isCooldownExpired user (command: Command) =
    let lastCommandTime =
        users.GetOrAdd((user, command.Name), (fun _ -> DateTime.MinValue.ToUniversalTime()))

    let timeSinceLastCommand = DateTime.UtcNow - lastCommandTime
    timeSinceLastCommand.TotalMilliseconds > command.Cooldown

let private executeCommand command parameters context =
    async {
        let! response = applyFunction command.Execute parameters context
        return response
    }

let private parseCommandAndParameters (message: string) =
    match message.Split(" ", StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
    | [] -> failwith "No command?"
    | [ command ] -> command, []
    | command :: parameters -> command, parameters

let private formatResponse (response: string) =
    if response.Length > 500 then
        response[..496] + "..."
    else
        response

let rec handleCommand userId username source message =
    async {
        let commandName, parameters = parseCommandAndParameters message

        let! user = getUser userId username

        match Map.tryFind commandName Commands.commands with
        | None -> return None
        | Some command ->
            if isCooldownExpired user command then
                users[(user, command.Name)] <- DateTime.UtcNow

                if (command.AdminOnly && not user.IsAdmin) then
                    return None
                else
                    let! response =
                        async {
                            let context = Context.createContext userId username user.IsAdmin source

                            match! executeCommand command parameters context with
                            | Ok value ->
                                match value with
                                | Message message -> return Some <| (Message <| formatResponse message)
                                | BotAction(action, message) -> return Some <| BotAction(action, formatResponse message)
                                | RunAlias command ->
                                    match! handleCommand userId username source command with
                                    | None -> return None
                                    | Some(response) -> return Some response
                                | Pipe commands ->
                                    let rec executePipe (acc: string) commands =
                                        match commands with
                                        | [] -> async { return Some <| Message acc }
                                        | c :: cs ->
                                            async {
                                                match! handleCommand userId username source $"{c} {acc}" with
                                                | None -> return None
                                                | Some(Message intermediateResult) ->
                                                    match! executePipe intermediateResult cs with
                                                    | None -> return None
                                                    | Some result -> return Some result
                                                | Some result -> return Some result
                                            }

                                    return! executePipe "" commands
                            | Error err -> return Some <| (Message <| formatResponse err)
                        }

                    return response
            else
                return None
    }

let safeHandleCommand userId username source message =
    async {
        try
            return! handleCommand userId username source message
        with ex ->
            Logging.error $"Unknown error occurred executing command" ex
            return None
    }
