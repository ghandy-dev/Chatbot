namespace Chatbot.Commands

[<AutoOpen>]
module Alias =

    open Chatbot.Database
    open Chatbot.Database.AliasRepository
    open Chatbot.Database.Types

    let private add userId name command =
        async {
            match! getByUserAndName (userId |> int) name with
            | Some _ -> return Ok <| Message $"Command {name} already exists."
            | None ->
                match! add (Alias.create (userId |> int) name (String.concat " " command)) with
                | DatabaseResult.Failure err -> return Error err.Message
                | DatabaseResult.Success 0 -> return Ok <| Message $"Command {name} already exists."
                | DatabaseResult.Success _ -> return Ok <| Message $"Command {name} successfully added."
        }

    let private edit userId name command =
        async {
            match! update (Alias.create (userId |> int) name (String.concat " " command)) with
            | DatabaseResult.Failure err -> return Error err.Message
            | DatabaseResult.Success 0 -> return Ok <| Message $"Command {name} does not exist."
            | DatabaseResult.Success _ -> return Ok <| Message $"Command {name} successfully updated."
        }

    let private delete userId name =
        async {
            match! delete (userId |> int) name with
            | DatabaseResult.Failure err -> return Error err.Message
            | DatabaseResult.Success 0 -> return Ok <| Message $"Command {name} does not exist."
            | DatabaseResult.Success _ -> return Ok <| Message $"Command {name} successfully removed."
        }

    let private run userId name =
        async {
            match! getByUserAndName (userId |> int) name with
            | None -> return Ok <| Message $"Command {name} does not exist."
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
            | "run" :: name :: parameters
            | name :: parameters -> return! run context.UserId name
            | [] -> return Ok <| Message "No definition provided"
        }
