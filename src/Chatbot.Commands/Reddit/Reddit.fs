namespace Chatbot.Commands.Reddit

[<AutoOpen>]
module Reddit =

    open Api
    open Chatbot.Authorization
    open Chatbot.Commands

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

    let private postFilter (p: Types.Thing<Types.T3>) =
        not <| p.Data.Over18 && not <| p.Data.IsSelf

    let reddit (args: string list) =
        async {
            match! tokenStore.GetToken TokenType.Reddit with
            | None -> return Message "Couldn't retrieve access token for Reddit API"
            | Some token ->
                match!
                    tryParseArgs args
                    |> Async.create
                    |> Result.bindAsync (fun (subreddit, sort) -> getPosts subreddit sort token)
                with
                | Error err -> return Message err
                | Ok subreddit ->
                    match subreddit.Data.Children with
                    | [] -> return Message "No posts found!"
                    | posts ->
                        let post =
                            posts
                            |> List.filter postFilter
                            |> fun ps -> ps |> List.randomChoice |> _.Data

                        let crosspostParent = post.CrosspostParentList |?? [] |> List.tryHead

                        match crosspostParent with
                        | Some cp ->
                            let title = (System.Web.HttpUtility.HtmlDecode post.Title).Replace("\n", "")
                            let url = System.Web.HttpUtility.HtmlDecode(cp.Url)
                            return Message $"r/{post.Subreddit} (x-posted from r/{cp.Subreddit}) \"{title}\" (+{post.Score}) {url}"
                        | None ->
                            let title = (System.Web.HttpUtility.HtmlDecode post.Title).Replace("\n", "")
                            let url = System.Web.HttpUtility.HtmlDecode(post.Url)
                            return Message $"r/{post.Subreddit} \"{title}\" (+{post.Score}) {url}"
        }
