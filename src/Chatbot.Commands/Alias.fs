namespace Commands

[<AutoOpen>]
module Alias =

    open FsToolkit.ErrorHandling

    open CommandError
    open Database
    open Database.AliasRepository

    let twitchService = Services.services.TwitchService

    let private validateCommand (command: string list) (commands: Map<string, _>) =
        let aliasCommands =
            command
            |> String.concat " "
            |> fun s -> s.Split(pipeSeperator)
            |> fun a ->
                a |> Array.map (fun s -> s.Split(" ", System.StringSplitOptions.TrimEntries ||| System.StringSplitOptions.RemoveEmptyEntries) |> Array.tryHead) |> Array.choose id

        match aliasCommands.Length with
        | 0 -> false
        | _ ->
            match aliasCommands |> Array.exists (fun ac -> commands |> Map.containsKey ac |> not) with
            | true -> false
            | false -> true


    let private add userId alias command commands =
        asyncResult {
            match validateCommand command commands with
            | false -> return! invalidArgs "Invalid command definition"
            | true ->
                match! AliasRepository.get (ByUserIdAliasName (int userId, alias)) with
                | Some _ -> return Message $"Alias {alias} already exists"
                | None ->
                    match! AliasRepository.add (Models.NewAlias.create (userId |> int) alias (String.concat " " command)) with
                    | DatabaseResult.Failure -> return! internalError "Error occured trying to add alias"
                    | DatabaseResult.Success 0 -> return Message $"You already have alias \"{alias}\""
                    | DatabaseResult.Success _ -> return Message $"Alias \"{alias}\" successfully added"
        }

    let private update userId alias command commands =
        asyncResult {
            match validateCommand command commands with
            | false -> return! invalidArgs "Invalid command definition"
            | true ->
                match! AliasRepository.update (Models.UpdateAlias.create (userId |> int) alias (String.concat " " command)) with
                | DatabaseResult.Failure -> return! internalError "Error occurred trying to update alias"
                | DatabaseResult.Success 0 -> return Message $"You don't have the alias \"{alias}\""
                | DatabaseResult.Success _ -> return Message $"Alias \"{alias}\" successfully updated"
        }

    let private delete userId alias =
        asyncResult {
            match! AliasRepository.delete (Models.DeleteAlias.create (userId |> int) alias) with
            | DatabaseResult.Failure -> return! internalError "Error occurred trying to delete alias"
            | DatabaseResult.Success 0 -> return Message $"You don't have the alias \"{alias}\""
            | DatabaseResult.Success _ -> return Message $"Alias \"{alias}\" successfully removed"
        }

    let private get (username: string) alias =
        asyncResult {
            match! twitchService.GetUser username |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User") with
            | None -> return Message "User not found"
            | Some user ->
                match! AliasRepository.get (ByUserIdAliasName (int user.Id, alias)) with
                | None ->
                    if strCompareIgnoreCase username user.Login then
                        return Message $"You don't have the alias \"{alias}\""
                    else
                        return Message $"{username} doesn't have the alias \"{alias}\""
                | Some alias -> return Message alias.Command
        }

    let private run userId alias parameters =
        asyncResult {
            match! AliasRepository.get (ByUserIdAliasName (int userId, alias)) with
            | None -> return Message $"You don't have the alias \"{alias}\""
            | Some alias -> return RunAlias (alias.Command, parameters)
        }

    let private copy sourceUsername targetUserId alias =
        asyncResult {
            let! user =
                twitchService.GetUser sourceUsername
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! source =
                AliasRepository.get (ByUserIdAliasName (int user.Id, alias))
                |> AsyncResult.requireSome (InvalidArgs $"{sourceUsername} doesn't have the alias \"{alias}\"")

            let! _ =
                AliasRepository.get (ByUserIdAliasName (int targetUserId, alias))
                |> AsyncResult.requireNone (InvalidArgs "You already have the alias \"{alias}\", use \"copyplace\" to replace an existing alias")

            match! AliasRepository.add (Models.NewAlias.create (targetUserId |> int) source.Name source.Command) with
            | DatabaseResult.Failure
            | DatabaseResult.Success 0 -> return! internalError "Error occured trying to add copied alias"
            | DatabaseResult.Success _ -> return Message $"Alias \"{source.Name}\" successfully copied"
        }

    let private copyPlace sourceUsername targetUserId alias =
        asyncResult {
            let! user =
                twitchService.GetUser sourceUsername
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! sourceAlias = AliasRepository.get (ByUserIdAliasName (int user.Id, alias))
            let! targetAlias = AliasRepository.get (ByUserIdAliasName (int targetUserId, alias))

            match sourceAlias, targetAlias with
            | None, _ -> return Message $"{sourceUsername} doesn't have the alias \"{alias}\""
            | Some sa, Some _ ->
                match! AliasRepository.update (Models.UpdateAlias.create (targetUserId |> int) sa.Name sa.Command) with
                | DatabaseResult.Failure
                | DatabaseResult.Success 0 -> return! internalError "Error occured trying to overwrite existing alias"
                | DatabaseResult.Success _ -> return Message $"Alias \"{sa.Name}\" successfully copied"
            | Some sa, None ->
                match! AliasRepository.add (Models.NewAlias.create (targetUserId |> int) sa.Name sa.Command) with
                | DatabaseResult.Failure
                | DatabaseResult.Success 0 -> return! internalError "Error occured trying to add copied alias"
                | DatabaseResult.Success _ -> return Message $"Alias \"{sa.Name}\" successfully copied"
        }

    let alias (context: Context) (commands: Map<string, _>) =
        asyncResult {
            match context.Args with
            | "add" :: alias :: command -> return! add context.UserId alias command commands
            | "remove" :: alias :: _
            | "delete" :: alias :: _ -> return! delete context.UserId alias
            | "edit" :: alias :: command
            | "update" :: alias :: command -> return! update context.UserId alias command commands
            | "copy" :: username :: alias :: _ -> return! copy username context.UserId alias
            | "copyplace" :: username :: alias :: _ -> return! copyPlace username context.UserId alias
            | [ "check" ; alias ]
            | [ "spy" ; alias ]
            | ["definition" ; alias ] -> return! get context.Username alias
            | "check" :: username :: alias :: _
            | "spy" :: username :: alias :: _
            | "definition" :: username :: alias :: _ -> return! get username alias
            | "run" :: alias :: parameters
            | alias :: parameters -> return! run context.UserId alias parameters
            | [] -> return! invalidArgs "Missing args"
        }
