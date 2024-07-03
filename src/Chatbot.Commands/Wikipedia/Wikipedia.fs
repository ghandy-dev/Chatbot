namespace Chatbot.Commands

[<AutoOpen>]
module Wikipedia =

    open Chatbot.Commands.Api.Wikipedia

    let wiki args =
        async {
            match args with
            | [] -> return Error "No input provided."
            | input ->
                let query = String.concat " " input

                match! getWikiPage query with
                | Error error -> return Error error
                | Ok pages ->
                    match pages.Pages |> List.tryHead with
                    | None -> return Error "No wikipedia results found."
                    | Some page -> return Ok <| Message $"https://en.wikipedia.org/wiki/{page.Key} {page.Description}"
        }
