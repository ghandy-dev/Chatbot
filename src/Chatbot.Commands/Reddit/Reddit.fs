namespace Chatbot.Commands

[<AutoOpen>]
module Reddit =

    open Chatbot.Authorization
    open Chatbot.Commands.Api.Reddit

    let private sortings = [ "top" ; "hot" ; "best" ] |> Set.ofList

    let private tryParseArgs args =
        match args with
        | [] -> Error "No subreddit specified"
        | [ subreddit ] -> Ok(subreddit, "hot")
        | sort :: subreddit :: _ ->
            if sortings |> Set.contains sort then
                Ok(subreddit, sort)
            else
                let validSortings = String.concat ", " sortings
                Error $"Valid sortings are {validSortings}"

    let reddit (args: string list) =
        async {
            match! tokenStore.GetToken TokenType.Reddit with
            | None -> return Error "Couldn't retrieve access token for Reddit API"
            | Some token ->
                match! tryParseArgs args |> AsyncResult.bindAsyncSync (fun (subreddit, sort) -> getPosts subreddit sort token) with
                | Error err -> return Error err
                | Ok subreddit ->
                    let posts = subreddit.Data.Children

                    if posts.Length = 0 then
                        return Ok <| Message "No posts found!"
                    else
                        let post =
                            posts
                            |> List.filter (fun p -> p.Data.Over18 = false && p.Data.IsSelf = false)
                            |> fun ps -> ps[System.Random.Shared.Next(ps.Length)].Data

                        return
                            Ok
                            <| Message $"r/{post.Subreddit} \"{System.Web.HttpUtility.HtmlDecode post.Title}\" (+{post.Score}) {post.Url}"
        }
