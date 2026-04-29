namespace Chatbot.Commands

[<AutoOpen>]
module Alias =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Twitch
    open Chatbot.Database
    open Chatbot.Database.Aliases

    let private validateCommand (command: string list) pipeSeparator (commands: Map<string, _>) =
        let aliasCommands =
            command
            |> String.concat " "
            |> fun s -> s |> strSplit pipeSeparator
            |> fun a ->
                a |> Array.map (fun s -> s.Split(" ", System.StringSplitOptions.TrimEntries ||| System.StringSplitOptions.RemoveEmptyEntries) |> Array.tryHead) |> Array.choose id

        match aliasCommands.Length with
        | 0 -> false
        | _ ->
            match aliasCommands |> Array.exists (fun ac -> commands |> Map.containsKey ac |> not) with
            | true -> false
            | false -> true


    let private add db pipeSeparator userId alias command commands =
        asyncResult {
            match validateCommand command pipeSeparator commands with
            | false -> return! invalidArgs "Invalid command definition"
            | true ->
                match! Aliases.get db (ByUserIdAliasName (int userId, alias)) with
                | Some _ -> return [ Message $"Alias {alias} already exists" ]
                | None ->
                    match! Aliases.add db (Models.NewAlias.create (userId |> int) alias (String.concat " " command)) with
                    | DatabaseResult.Failure -> return! internalError "Error occured trying to add alias"
                    | DatabaseResult.Success 0 -> return [ Message $"You already have alias \"{alias}\"" ]
                    | DatabaseResult.Success _ -> return [ Message $"Alias \"{alias}\" successfully added" ]
        }

    let private update db pipeSeparator userId alias command commands =
        asyncResult {
            match validateCommand command pipeSeparator commands with
            | false -> return! invalidArgs "Invalid command definition"
            | true ->
                match! Aliases.update db (Models.UpdateAlias.create (userId |> int) alias (String.concat " " command)) with
                | DatabaseResult.Failure -> return! internalError "Error occurred trying to update alias"
                | DatabaseResult.Success 0 -> return [ Message $"You don't have the alias \"{alias}\"" ]
                | DatabaseResult.Success _ -> return [ Message $"Alias \"{alias}\" successfully updated" ]
        }

    let private delete db userId alias =
        asyncResult {
            match! Aliases.delete db (Models.DeleteAlias.create (userId |> int) alias) with
            | DatabaseResult.Failure -> return! internalError "Error occurred trying to delete alias"
            | DatabaseResult.Success 0 -> return [ Message $"You don't have the alias \"{alias}\"" ]
            | DatabaseResult.Success _ -> return [ Message $"Alias \"{alias}\" successfully removed" ]
        }

    let private definition db (twitchService: TwitchService) (username: string) alias =
        asyncResult {
            match! twitchService.Users.GetUser username |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User") with
            | None -> return [ Message "User not found" ]
            | Some user ->
                match! Aliases.get db (ByUserIdAliasName (int user.Id, alias)) with
                | None ->
                    if strCompareIgnoreCase username user.Login then
                        return [ Message $"You don't have the alias \"{alias}\"" ]
                    else
                        return [ Message $"{username} doesn't have the alias \"{alias}\"" ]
                | Some alias -> return [ Message alias.Command ]
        }

    let private copy db (twitchService: TwitchService) sourceUsername targetUserId alias =
        asyncResult {
            let! user =
                twitchService.Users.GetUser sourceUsername
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! source =
                Aliases.get db (ByUserIdAliasName (int user.Id, alias))
                |> AsyncResult.requireSome (InvalidArgs $"{sourceUsername} doesn't have the alias \"{alias}\"")

            let! _ =
                Aliases.get db (ByUserIdAliasName (int targetUserId, alias))
                |> AsyncResult.requireNone (InvalidArgs "You already have the alias \"{alias}\", use \"copyplace\" to replace an existing alias")

            match! Aliases.add db (Models.NewAlias.create (targetUserId |> int) source.Name source.Command) with
            | DatabaseResult.Failure
            | DatabaseResult.Success 0 -> return! internalError "Error occured trying to add copied alias"
            | DatabaseResult.Success _ -> return [ Message $"Alias \"{source.Name}\" successfully copied" ]
        }

    let private copyPlace db (twitchService: TwitchService) sourceUsername targetUserId alias =
        asyncResult {
            let! user =
                twitchService.Users.GetUser sourceUsername
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! sourceAlias = Aliases.get db (ByUserIdAliasName (int user.Id, alias))
            let! targetAlias = Aliases.get db (ByUserIdAliasName (int targetUserId, alias))

            match sourceAlias, targetAlias with
            | None, _ -> return [ Message $"{sourceUsername} doesn't have the alias \"{alias}\"" ]
            | Some sa, Some _ ->
                match! Aliases.update db (Models.UpdateAlias.create (targetUserId |> int) sa.Name sa.Command) with
                | DatabaseResult.Failure
                | DatabaseResult.Success 0 -> return! internalError "Error occured trying to overwrite existing alias"
                | DatabaseResult.Success _ -> return [ Message $"Alias \"{sa.Name}\" successfully copied" ]
            | Some sa, None ->
                match! Aliases.add db (Models.NewAlias.create (targetUserId |> int) sa.Name sa.Command) with
                | DatabaseResult.Failure
                | DatabaseResult.Success 0 -> return! internalError "Error occured trying to add copied alias"
                | DatabaseResult.Success _ -> return [ Message $"Alias \"{sa.Name}\" successfully copied" ]
        }

    let alias db pipeSeparator (twitchService: TwitchService) (context: Context) (commands: Map<string, Command>) =
        asyncResult {
            match context.MessageArgs with
            | "add" :: alias :: command -> return! add db pipeSeparator context.UserId alias command commands
            | "remove" :: alias :: _
            | "delete" :: alias :: _ -> return! delete db context.UserId alias
            | "edit" :: alias :: command
            | "update" :: alias :: command -> return! update db pipeSeparator context.UserId alias command commands
            | "copy" :: username :: alias :: _ -> return! copy db twitchService username context.UserId alias
            | "copyplace" :: username :: alias :: _ -> return! copyPlace db twitchService username context.UserId alias
            | [ "check" ; alias ]
            | [ "spy" ; alias ]
            | ["definition" ; alias ] -> return! definition db twitchService context.Username alias
            | "check" :: username :: alias :: _
            | "spy" :: username :: alias :: _
            | "definition" :: username :: alias :: _ -> return! definition db twitchService username alias
            | _ -> return! invalidArgs "Missing args"
        }
