module Chatbot.Commands.Handler

open Chatbot
open Chatbot.Commands
open Chatbot.Database
open Chatbot.Database.Types

open System

let private logger = Logging.createNamedLogger "Commands"

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
            | DatabaseResult.Failure ex -> return failwith ex.Message
            | DatabaseResult.Success _ ->
                match! UserRepository.getById (userId |> int) with
                | None -> return failwith "Couldn't retrieve new user"
                | Some user -> return user
        | Some user -> return user
    }

let private isCooldownExpired user command =
    let lastCommandTime =
        users.GetOrAdd((user, command.Name), (fun _ -> DateTime.MinValue.ToUniversalTime()))

    let timeSinceLastCommand = DateTime.UtcNow - lastCommandTime
    timeSinceLastCommand.TotalMilliseconds > command.Cooldown

let private executeCommand command parameters context =
    async {
        let! response = applyFunction command.Execute parameters context

        match response with
        | Error message -> logger.LogInfo $"error: {message}, command {command.Name}, failed. parameters: {parameters}, context: {context}"
        | _ -> ()

        return response
    }

let private parseCommandAndParameters (message: string) =
    let parts =
        message.Split(" ", StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

    let command = parts[0]
    let parameters = if parts.Length > 1 then parts[1..] else []

    command, parameters

let private formatResponse (response: string) =
    if response.Length > 500 then
        response[..496] + "..."
    else
        response

let private executeAndFormatResponse command parameters context =
    async {
        match! executeCommand command parameters context with
        | Ok outcome ->
            match outcome with
            | BotAction(action, message) -> return BotAction(action, formatResponse message)
            | Message message -> return Message <| formatResponse message
        | Error err -> return Message <| formatResponse err
    }

let handleCommand userId username source message =
    async {
        let commandName, parameters = parseCommandAndParameters message

        let! user = getUser userId username

        match Map.tryFind commandName Commands.commands with
        | None -> return None
        | Some command ->
            if isCooldownExpired user command then
                users[(user, command.Name)] <- DateTime.UtcNow

                let context = Context.createContext userId username user.IsAdmin source

                try
                    let! response =
                        match command, user with
                        | c, user when c.AdminOnly && user.IsAdmin -> executeAndFormatResponse c parameters context
                        | c, _ -> executeAndFormatResponse c parameters context

                    return Some response
                with ex ->
                    logger.LogError($"Error occured running command: {command} {context} {parameters}", ex)
                    return None
            else
                return None
    }
