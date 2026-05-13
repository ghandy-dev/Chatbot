namespace Chatbot.Commands

[<AutoOpen>]
module Alias =

    // open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Domain.Types
    open Chatbot.Core.Services.Twitch

    let private validateCommand (command: string list) pipeSeparator (commands: Map<string, _>) =
        let aliasCommands =
            command
            |> String.join " "
            |> String.split pipeSeparator
            |> Array.map (fun s -> s.Split(" ", System.StringSplitOptions.TrimEntries ||| System.StringSplitOptions.RemoveEmptyEntries) |> Array.tryHead)
            |> Array.choose id

        match aliasCommands.Length with
        | 0 -> invalidArgs "Invalid command definition"
        | _ ->
            match aliasCommands |> Array.exists (fun ac -> commands |> Map.containsKey ac |> not) with
            | true -> invalidArgs "Invalid command definition"
            | false -> Ok (String.join " " command)


    let private add pipeSeparator (aliasRepo: IAliasRepository) userId alias command commands =
        asyncResult {
            let! command = validateCommand command pipeSeparator commands

            match! aliasRepo.Get userId alias with
            | Some _ -> return [ Message $"Alias {alias} already exists" ]
            | None ->
                let newAlias = NewAlias.create (userId |> int) alias command

                return!
                    aliasRepo.Add newAlias
                    |> AsyncResult.eitherMap
                        (fun ok ->
                            match ok with
                            | 0 -> [ Message $"You already have alias \"{alias}\"" ]
                            | _ -> [ Message $"Alias \"{alias}\" successfully added" ]
                        )
                        (fun _ -> InternalError "Error occured trying to add alias")
        }

    let private update pipeSeparator (aliasRepo: IAliasRepository) userId alias command commands =
        asyncResult {
            let! command = validateCommand command pipeSeparator commands

            let updatedAlias = UpdateAlias.create (userId |> int) alias command

            return!
                aliasRepo.Update updatedAlias
                |> AsyncResult.eitherMap
                    (fun ok ->
                        match ok with
                        | 0 -> [ Message $"You don't have the alias \"{alias}\"" ]
                        | _ -> [ Message $"Alias \"{alias}\" successfully updated" ]
                    )
                    (fun _ -> InternalError "Error occurred trying to update alias")
                }

    // |> AsyncResult.eitherMap
    //     (fun ok ->

    //     )
    //     (fun _ -> )

    let private delete (aliasRepo: IAliasRepository) userId alias =
        asyncResult {
            return!
                aliasRepo.Delete alias (int userId)
                |> AsyncResult.eitherMap
                    (fun ok ->
                        match ok with
                        | 0 -> [ Message $"You don't have the alias \"{alias}\"" ]
                        | _ -> [ Message $"Alias \"{alias}\" successfully removed" ]
                    )
                    (fun _ -> InternalError "Error occurred trying to delete alias")
        }

    let private definition (twitchService: TwitchService) (aliasRepo: IAliasRepository) (username: string) alias =
        asyncResult {
            let! user =
                twitchService.Users.GetUser username
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            match! aliasRepo.Get (int user.Id) alias with
            | None ->
                if String.compareIgnoreCase username user.Login then
                    return [ Message $"You don't have the alias \"{alias}\"" ]
                else
                    return [ Message $"{username} doesn't have the alias \"{alias}\"" ]
            | Some alias -> return [ Message alias.Command ]
        }

    let private copy (twitchService: TwitchService) (aliasRepo: IAliasRepository) sourceUsername targetUserId alias =
        asyncResult {
            let! user =
                twitchService.Users.GetUser sourceUsername
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! source =
                aliasRepo.Get (int user.Id) alias
                |> AsyncResult.requireSome (InvalidArgs $"{sourceUsername} doesn't have the alias \"{alias}\"")

            let! _ =
                aliasRepo.Get (int targetUserId) alias
                |> AsyncResult.requireNone (InvalidArgs "You already have the alias \"{alias}\", use \"copyplace\" to replace an existing alias")

            let copiedAlias = NewAlias.create (targetUserId |> int) source.Name source.Command

            return!
                async {
                    match! aliasRepo.Add copiedAlias with
                    | Error _
                    | Ok 0 -> return internalError "Error occured trying to add copied alias"
                    | Ok _ -> return Ok [ Message $"Alias \"{source.Name}\" successfully copied" ]
                }
        }

    let private copyPlace (twitchService: TwitchService) (aliasRepo: IAliasRepository) sourceUsername targetUserId alias =
        asyncResult {
            let! user =
                twitchService.Users.GetUser sourceUsername
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Twitch - User")
                |> AsyncResult.bindRequireSome (InvalidArgs "User not found")

            let! sourceAlias =
                aliasRepo.Get (int user.Id) alias
                |> Async.bind (FSharpPlus.Option.toResult >> Async.singleton)
                |> AsyncResult.mapError (fun _ -> InvalidArgs $"{sourceUsername} doesn't have the alias \"{alias}\"")

            let targetAlias = aliasRepo.Get (int targetUserId) alias

            return!
                async {
                    match! targetAlias with
                    | Some _ ->
                        let updatedAlias = UpdateAlias.create (targetUserId |> int) sourceAlias.Name sourceAlias.Command

                        match! aliasRepo.Update updatedAlias with
                        | Error _
                        | Ok 0 -> return internalError "Error occured trying to overwrite existing alias"
                        | Ok _ -> return Ok [ Message $"Alias \"{sourceAlias.Name}\" successfully copied" ]
                    | None ->
                        let copiedAlias = NewAlias.create (targetUserId |> int) sourceAlias.Name sourceAlias.Command

                        match! aliasRepo.Add copiedAlias with
                        | Error _
                        | Ok 0 -> return internalError "Error occured trying to add copied alias"
                        | Ok _ -> return Ok [ Message $"Alias \"{sourceAlias.Name}\" successfully copied" ]
                }
        }

    let alias pipeSeparator (aliasRepo: IAliasRepository) (twitchService: TwitchService) (context: Context) (commands: Map<string, Command>) =
        asyncResult {
            match context.MessageArgs with
            | "add" :: alias :: command -> return! add pipeSeparator aliasRepo (int context.UserId) alias command commands
            | "remove" :: alias :: _
            | "delete" :: alias :: _ -> return! delete aliasRepo (int context.UserId) alias
            | "edit" :: alias :: command
            | "update" :: alias :: command -> return! update pipeSeparator aliasRepo (int context.UserId) alias command commands
            | "copy" :: username :: alias :: _ -> return! copy twitchService aliasRepo username (int context.UserId) alias
            | "copyplace" :: username :: alias :: _ -> return! copyPlace twitchService aliasRepo username (int context.UserId) alias
            | [ "check" ; alias ]
            | [ "spy" ; alias ]
            | ["definition" ; alias ] -> return! definition twitchService aliasRepo context.Username alias
            | "check" :: username :: alias :: _
            | "spy" :: username :: alias :: _
            | "definition" :: username :: alias :: _ -> return! definition twitchService aliasRepo username alias
            | _ -> return! invalidArgs "Missing args"
        }
