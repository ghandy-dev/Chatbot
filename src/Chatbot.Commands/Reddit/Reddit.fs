namespace Chatbot.Commands

[<AutoOpen>]
module Reddit =

    open Chatbot.Commands.Api.Reddit

    let private sortings = [ "top" ; "hot" ; "best" ]

    let private valid args =
        match args with
        | [] -> Error "No subreddit specified"
        | subreddit :: sort :: _ ->
            if sortings |> List.contains sort then
                Ok(subreddit, sort)
            else
                let validSortings = String.concat ", " sortings
                Error $"Valid sortings are {validSortings}"
        | subreddit :: _ -> Ok(subreddit, "hot")

    let reddit (args: string list) =
        async {
            match!
                valid args
                |> AsyncResult.zipAsyncSync (getAccessToken ())
                |> AsyncResult.bind (fun (token, (subreddit, sort)) -> getPosts subreddit sort token)
            with
            | Error error -> return Error error
            | Ok posts ->
                let post =
                    posts.Data.Children
                    |> List.filter (fun p -> p.Data.Over18 = false && p.Data.IsSelf = false)
                    |> fun ps -> ps[System.Random.Shared.Next(ps.Length)].Data

                return Ok <| Message $"r/{post.Subreddit} \"{System.Web.HttpUtility.HtmlDecode post.Title}\" (+{post.Score}) {post.Url}"
        }
