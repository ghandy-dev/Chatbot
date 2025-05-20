module Commands.Handler

open Commands
open Database
open Shared

open System

let private parseCommandAndArgs (message: string) =
    match message.Split(" ", StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
    | [] -> None
    | [ command ] -> Some (command, [])
    | command :: parameters -> Some (command, parameters)

let private hasCooldownExpired (user: Models.User) (command: Command) =
    let lastCommandTime = userCommandCooldowns.GetOrAdd((user, command.Name), (fun _ -> DateTime.MinValue.ToUniversalTime()))
    let timeSinceLastCommand = DateTime.UtcNow - lastCommandTime
    timeSinceLastCommand.TotalMilliseconds > command.Cooldown

let private applyFunction =
    function
    | S f -> fun _ _ _ -> async { return f () }
    | SA f -> fun args _ _ -> async { return f args }
    | SAC f -> fun args ctx _ -> async { return f args ctx }
    | A f -> fun _ _ _ -> f ()
    | AA f -> fun args _ _ -> f args
    | AAC f -> fun args ctx _ -> f args ctx
    | AACM f -> fun args ctx cmd -> f args ctx cmd

let private executeCommand (command: Command) (args: Args) (context: Context) =
    async {
        let! response = applyFunction command.Execute args context Commands.commands
        return response
    }

let private getOrAddUser userId username =
    async {
        match! UserRepository.get (int userId) with
        | None ->
            match! UserRepository.add (Models.NewUser.create (int userId) username) with
            | DatabaseResult.Failure -> Logging.error "Error adding user" (new exn())
            | _ -> ()
            return Models.User.create (int userId) username
        | Some user -> return user
    }

let rec private handleCommand (userId: string) (username: string) (source: MessageSource) (message: string) (parsedEmotes: Map<string, string>) =
    async {
        let maybeCommandAndArgs: (Command * string list) option =
            message.Replace("\U000e0000", "")
            |> parseCommandAndArgs
            |> Option.bind (fun (command, args) ->
                Map.tryFind command Commands.commands
                |> Option.bind (fun command' -> Some (command', args))
            )

        let! user = getOrAddUser userId username

        let messageEmotes = parsedEmotes |> Map.map (fun _ v -> $"https://static-cdn.jtvnw.net/emoticons/v2/%s{v}/static/dark/3.0")

        let channel =
            match source with
            | Channel c -> Some c
            | Whisper _ -> None

        match maybeCommandAndArgs with
        | None -> return None
        | Some (command, args) ->
            if hasCooldownExpired user command then
                userCommandCooldowns[(user, command.Name)] <- DateTime.UtcNow

                if command.AdminOnly && not user.IsAdmin then
                    return None
                else
                    let! response =
                        async {
                            let context =
                                Context.create userId username user.IsAdmin source {
                                    GlobalEmotes = emoteService.GlobalEmotes
                                    ChannelEmotes =
                                        channel
                                        |> Option.bind (fun c -> emoteService.ChannelEmotes |> Dictionary.tryGetValue c.RoomId)
                                        |?? []
                                    MessageEmotes = messageEmotes
                                }

                            match! executeCommand command args context with
                            | Message message -> return Some <| (Message <| formatChatMessage message)
                            | BotAction(action, Some message) -> return Some <| BotAction(action, Some (formatChatMessage message))
                            | BotAction(action, None) -> return Some <| BotAction(action, None)
                            | RunAlias(command, parameters) ->
                                let formattedCommand = Text.formatString command parameters

                                match! handleCommand userId username source formattedCommand parsedEmotes with
                                | None -> return None
                                | Some response -> return Some response
                            | Pipe commands ->
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
            Logging.error $"Error occurred executing command" ex
            return None
    }
