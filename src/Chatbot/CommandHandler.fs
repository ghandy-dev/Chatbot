module Commands.Handler

open System

open FSharpPlus

open Commands
open Database
open Shared

let private parseCommandAndArgs (message: string) =
    match message.Split(" ", StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
    | [] -> None
    | [ command ] -> Some (command, [])
    | command :: parameters -> Some (command, parameters)

let private hasCooldownExpired (user: Models.User) (command: Command) =
    let lastCommandTime = userCommandCooldowns.GetOrAdd((user, command.Name), (fun _ -> DateTime.MinValue.ToUniversalTime()))
    let timeSinceLastCommand = utcNow() - lastCommandTime
    timeSinceLastCommand.TotalMilliseconds > command.Cooldown

let private applyFunction =
    function
    | Sync f -> fun context -> async { return f context }
    | Async f -> fun context -> f context

let private executeCommand (command: Command) (args: Args) (context: Context) =
    async {
        let! response = applyFunction command.Execute context
        return response
    }

let private getOrAddUser userId username =
    async {
        match! UserRepository.get (int userId) with
        | None ->
            match! UserRepository.add (Models.NewUser.create (int userId) username) with
            | DatabaseResult.Failure -> Logging.errorEx "Error adding user" (new exn())
            | _ -> ()
            return Models.User.create (int userId) username
        | Some user -> return user
    }

let rec private handleCommand (userId: string) (username: string) (source: MessageSource) (message: string) (parsedEmotes: Map<string, string>) =
    async {
        let maybeCommandAndArgs: (Command * string list) option =
            message
            |> strReplace "\U000e0000" ""
            |> strReplace "\ue34f" ""
            |> parseCommandAndArgs
            |> Option.bind (fun (command, args) ->
                Map.tryFind command Commands.commands
                |> Option.map (fun command' -> command', args)
            )

        match maybeCommandAndArgs with
        | None -> return None
        | Some (command, args) ->
            let! user = getOrAddUser userId username

            let channel =
                match source with
                | Channel c -> Some c
                | Whisper _ -> None

            if hasCooldownExpired user command then
                userCommandCooldowns[(user, command.Name)] <- utcNow()

                if command.AdminOnly && not user.IsAdmin then
                    return None
                else
                    let! response =
                        async {
                            let channelEmotes =
                                channel
                                |> Option.bind (fun c -> Services.services.EmoteService.ChannelEmotes |> Dictionary.tryGetValue c.RoomId)
                                |? []

                            let messageEmotes = parsedEmotes |> Map.map (fun _ v -> $"https://static-cdn.jtvnw.net/emoticons/v2/%s{v}/static/dark/3.0")

                            let emotes =
                                EmoteProviders.Emotes.create
                                    Services.services.EmoteService.GlobalEmotes
                                    channelEmotes
                                    messageEmotes

                            let context = Context.create args userId username user.IsAdmin source emotes

                            match! executeCommand command args context with
                            | Ok (Message message) -> return Some <| Message message
                            | Ok (BotAction(action, Some message)) -> return Some <| BotAction(action, Some message)
                            | Ok (BotAction(action, None)) -> return Some <| BotAction(action, None)
                            | Ok (RunAlias(command, parameters)) ->
                                let formattedCommand = strFormat command parameters

                                match! handleCommand userId username source formattedCommand parsedEmotes with
                                | None -> return None
                                | Some response -> return Some response
                            | Ok (Pipe commands) ->
                                let rec executePipe (acc: string) commands =
                                    match commands with
                                    | [] -> async { return Some <| Message acc }
                                    | c :: cs ->
                                        async {
                                            match! handleCommand userId username source $"{c} {acc}" parsedEmotes with
                                            | None -> return None
                                            | Some(Message intermediateResult) ->
                                                match! executePipe intermediateResult cs with
                                                | None -> return None
                                                | Some result -> return Some result
                                            | Some result -> return Some result
                                        }

                                return! executePipe "" commands
                            | Error error -> return Some (Message <| CommandError.toMessage error)
                        }

                    return response
            else
                return None
    }

let safeHandleCommand (userId: string) (username: string) (source: MessageSource) (message: string) (parsedEmotes: Map<string, string>) =
    async {
        try
            return! handleCommand userId username source message parsedEmotes
        with ex ->
            Logging.errorEx $"Error occurred executing command" ex
            return None
    }
