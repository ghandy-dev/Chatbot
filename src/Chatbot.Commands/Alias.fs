namespace Chatbot.Commands

[<AutoOpen>]
module Alias =

    open Chatbot.Database
    open Chatbot.Database.AliasRepository
    open Chatbot.Database.Types

    let private add userId name command =
        async {
            match! getByUserAndName (userId |> int) name with
            | Some _ -> return Ok <| Message $"Alias {name} already exists."
            | None ->
                match! add (Alias.create (userId |> int) name (String.concat " " command)) with
                | DatabaseResult.Failure -> return Error "Error occured trying to add alias."
                | DatabaseResult.Success 0 -> return Ok <| Message $"You already have alias {name}."
                | DatabaseResult.Success _ -> return Ok <| Message $"Alias {name} successfully added."
        }

    let private edit userId name command =
        async {
            match! update (Alias.create (userId |> int) name (String.concat " " command)) with
            | DatabaseResult.Failure -> return Error "Error occurred trying to edit alias."
            | DatabaseResult.Success 0 -> return Ok <| Message $"You don't have the alias {name}."
            | DatabaseResult.Success _ -> return Ok <| Message $"Alias {name} successfully updated."
        }

    let private delete userId name =
        async {
            match! delete (userId |> int) name with
            | DatabaseResult.Failure -> return Error "Error occurred trying to delete alias."
            | DatabaseResult.Success 0 -> return Ok <| Message $"You don't have the alias {name}."
            | DatabaseResult.Success _ -> return Ok <| Message $"Alias {name} successfully removed."
        }

    let private get userId name =
        async {
            match! getByUserAndName (userId |> int) name with
            | None -> return Ok <| Message $"You don't have the alias {name}."
            | Some alias -> return Ok <| Message alias.Command
        }

    let private run userId name =
        async {
            match! getByUserAndName (userId |> int) name with
            | None -> return Ok <| Message $"You don't have the alias {name}."
            | Some alias -> return Ok <| RunAlias alias.Command
        }

    let alias args (context: Context) =
        async {
            match args |> List.ofSeq with
            | "add" :: name :: command -> return! add context.UserId name command
            | "remove" :: name :: _
            | "delete" :: name :: _ -> return! delete context.UserId name
            | "edit" :: name :: command
            | "update" :: name :: command -> return! edit context.UserId name command
            | "definition" :: name :: _ -> return! get context.UserId name
            | "run" :: name :: parameters
            | name :: parameters -> return! run context.UserId name
            | [] -> return Ok <| Message "No definition provided"
        }
