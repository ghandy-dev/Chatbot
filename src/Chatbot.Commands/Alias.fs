namespace Chatbot.Commands

[<AutoOpen>]
module Alias =

    open Chatbot.Database
    open Chatbot.Database.Types.Aliases

    let private add userId alias command =
        async {
            match! AliasRepository.get (userId |> int) alias with
            | Some _ -> return Message $"Alias {alias} already exists"
            | None ->
                match! AliasRepository.add (Alias.create (userId |> int) alias (String.concat " " command)) with
                | DatabaseResult.Failure -> return Message "Error occured trying to add alias"
                | DatabaseResult.Success 0 -> return Message $"You already have alias \"{alias}\""
                | DatabaseResult.Success _ -> return Message $"Alias \"{alias}\" successfully added"
        }

    let private update userId alias command =
        async {
            match! AliasRepository.update (Alias.create (userId |> int) alias (String.concat " " command)) with
            | DatabaseResult.Failure -> return Message "Error occurred trying to update alias"
            | DatabaseResult.Success 0 -> return Message $"You don't have the alias \"{alias}\""
            | DatabaseResult.Success _ -> return Message $"Alias \"{alias}\" successfully updated"
        }

    let private delete userId alias =
        async {
            match! AliasRepository.delete (userId |> int) alias with
            | DatabaseResult.Failure -> return Message "Error occurred trying to delete alias"
            | DatabaseResult.Success 0 -> return Message $"You don't have the alias \"{alias}\""
            | DatabaseResult.Success _ -> return Message $"Alias \"{alias}\" successfully removed"
        }

    let private get (username: string) alias =
        async {
            match! TTVSharp.Helix.Users.getUser username with
            | None -> return Message "User not found"
            | Some user ->
                match! AliasRepository.get (user.Id |> int) alias with
                | None ->
                    if System.String.Compare(username, user.Login, true) = 0 then
                        return Message $"You don't have the alias \"{alias}\""
                    else
                        return Message $"{username} doesn't have the alias \"{alias}\""
                | Some alias -> return Message alias.Command
        }

    let private run userId alias parameters =
        async {
            match! AliasRepository.get (userId |> int) alias with
            | None -> return Message $"You don't have the alias \"{alias}\""
            | Some alias -> return RunAlias (alias.Command, parameters)
        }

    let private copy sourceUsername targetUserId alias =
        async {
            match! TTVSharp.Helix.Users.getUser sourceUsername with
            | None -> return Message "User not found"
            | Some user ->

                let! sourceAlias = AliasRepository.get (user.Id |> int) alias
                let! targetAlias = AliasRepository.get (targetUserId |> int) alias

                match sourceAlias, targetAlias with
                | None, _ -> return Message $"{sourceUsername} doesn't have the alias \"{alias}\""
                | Some sa, Some _ ->
                    return Message $"You already have the alias \"{sa.Name}\", use \"copyplace\" to replace an existing alias"
                | Some sa, None ->
                    let copiedTa = { sa with UserId = targetUserId |> int}
                    match! AliasRepository.add copiedTa with
                    | DatabaseResult.Failure
                    | DatabaseResult.Success 0 -> return Message "Error occured trying to add copied alias"
                    | DatabaseResult.Success _ -> return Message $"Alias \"{sa.Name}\" successfully copied"
        }

    let private copyPlace sourceUsername targetUserId alias =
        async {
            match! TTVSharp.Helix.Users.getUser sourceUsername with
            | None -> return Message "User not found"
            | Some user ->

                let! sourceAlias = AliasRepository.get (user.Id |> int) alias
                let! targetAlias = AliasRepository.get (targetUserId |> int) alias

                match sourceAlias, targetAlias with
                | None, _ -> return Message $"{sourceUsername} doesn't have the alias \"{alias}\""
                | Some sa, Some _ ->
                    let copiedTa = { sa with UserId = targetUserId |> int}
                    match! AliasRepository.update copiedTa with
                    | DatabaseResult.Failure
                    | DatabaseResult.Success 0 -> return Message "Error occured trying to overwrite existing alias"
                    | DatabaseResult.Success _ -> return Message $"Alias \"{sa.Name}\" successfully copied"
                | Some sa, None ->
                    let copiedTa = { sa with UserId = targetUserId |> int}
                    match! AliasRepository.add copiedTa with
                    | DatabaseResult.Failure
                    | DatabaseResult.Success 0 -> return Message "Error occured trying to add copied alias"
                    | DatabaseResult.Success _ -> return Message $"Alias \"{sa.Name}\" successfully copied"
        }

    let alias args (context: Context) =
        async {
            match args with
            | "add" :: alias :: command -> return! add context.UserId alias command
            | "remove" :: alias :: _
            | "delete" :: alias :: _ -> return! delete context.UserId alias
            | "edit" :: alias :: command
            | "update" :: alias :: command -> return! update context.UserId alias command
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
            | [] -> return Message "No definition provided"
        }
