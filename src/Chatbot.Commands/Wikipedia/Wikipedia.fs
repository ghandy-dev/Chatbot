namespace Commands

[<AutoOpen>]
module Wikipedia =

    open FsToolkit.ErrorHandling

    open CommandError
    open Wikipedia.Api

    let wiki args =
        asyncResult {
            match args with
            | [] -> return! invalidArgs "No input provided."
            | input ->
                let query = String.concat " " input
                let! pages = getWikiResults query |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Wikipedia")

                match pages.Pages with
                | [] -> return Message "No wikipedia page found!"
                | page :: _ ->
                    return Message $"https://en.wikipedia.org/wiki/{page.Key} {page.Description}"
        }
