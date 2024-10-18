module Commands.Handler

open Commands
open Database
open Database.Types.Users
open State

open System

let private applyFunction =
    function
    | S f -> fun _ _ _ -> async { return f () }
    | SA f -> fun args _ _ -> async { return f args }
    | SAC f -> fun args ctx _ -> async { return f args ctx }
    | A f -> fun _ _ _ -> f ()
    | AA f -> fun args _ _ -> f args
    | AAC f -> fun args ctx _ -> f args ctx
    | AACM f -> fun args ctx cmd -> f args ctx cmd

let private getUser (userId: string) username =
    async {
        match! UserRepository.getByUserId (userId |> int) with
        | None ->
            let user = User.create (userId |> int) username
            UserRepository.add user |> Async.Ignore |> ignore
            return user
        | Some user -> return user
    }

let private cooldownExpired user (command: Command) =
    let lastCommandTime =
        userCommandCooldowns.GetOrAdd((user, command.Name), (fun _ -> DateTime.MinValue.ToUniversalTime()))

    let timeSinceLastCommand = DateTime.UtcNow - lastCommandTime
    timeSinceLastCommand.TotalMilliseconds > command.Cooldown

let private executeCommand command parameters context =
    async {
        let! response = applyFunction command.Execute parameters context Commands.commands
        return response
    }

let private parseCommandAndParameters (message: string) =
    match
        message.Replace("\U000e0000", "")
        |> fun m -> m.Split(" ", StringSplitOptions.RemoveEmptyEntries)
        |> List.ofArray
    with
    | [] -> failwith "Empty message, expected command"
    | [ command ] -> command, []
    | command :: parameters -> command, parameters

let rec private handleCommand userId username source message =
    async {
        let commandName, parameters = parseCommandAndParameters message

        let! user = getUser userId username

        let channel =
            match source with
            | Channel c -> Some c
            | Whisper _ -> None

        match Map.tryFind commandName Commands.commands with
        | None -> return None
        | Some command ->
            if cooldownExpired user command then
                userCommandCooldowns[(user, command.Name)] <- DateTime.UtcNow

                if command.AdminOnly && not user.IsAdmin then
                    return None
                else
                    let! response =
                        async {
                            let context =
                                Context.createContext
                                    userId
                                    username
                                    user.IsAdmin
                                    source
                                    { GlobalEmotes = emoteService.GlobalEmotes
                                      ChannelEmotes = channel
                                        |> Option.bind (fun c ->
                                            emoteService.ChannelEmotes
                                            |> ConcurrentDictionary.tryGetValue c.RoomId
                                            |> Option.orElse (Some Emotes.Emotes.empty
                                        )
                                    )}

                            match! executeCommand command parameters context with
                            | Message message -> return Some <| (Message <| formatChatMessage message)
                            | BotAction(action, message) -> return Some <| BotAction(action, formatChatMessage message)
                            | RunAlias(command, parameters) ->
                                let formattedCommand = Utils.Text.formatString command parameters

                                match! handleCommand userId username source formattedCommand with
                                | None -> return None
                                | Some response -> return Some response
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
            Logging.error $"Error occurred executing command" ex
            return None
    }
