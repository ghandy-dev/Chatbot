namespace Commands

[<AutoOpen>]
module Wikipedia =

    open Commands.Api.Wikipedia

    let wiki args =
        async {
            match args with
            | [] -> return Message "No input provided."
            | input ->
                let query = String.concat " " input

                match! getWikiPage query with
                | Error err -> return Message err
                | Ok pages ->
                    match pages.Pages |> List.tryHead with
                    | None -> return Message "No wikipedia results found."
                    | Some page -> return Message $"https://en.wikipedia.org/wiki/{page.Key} {page.Description}"
        }
