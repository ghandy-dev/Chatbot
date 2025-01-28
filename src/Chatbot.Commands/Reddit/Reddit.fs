namespace Commands

[<AutoOpen>]
module Reddit =

    open Authorization
    open Commands
    open Commands.Reddit.Api
    open Commands.Reddit.Types

    let redditKeys = [ "sort" ; "flair" ]

    let private postFilter (p: Thing<T3>) =
        not <| p.Data.Over18 && not <| p.Data.IsSelf

    let private flairFilter (flair: string) (p: Thing<T3>) =
        p.Data.Flair |?? "" |> fun f -> System.String.Compare(f, flair, true) = 0

    let reddit (args: string list) =
        async {
            match args with
            | [] -> return Message "No subreddit specified"
            | args ->
                let keyValues = KeyValueParser.parse args redditKeys
                let args = KeyValueParser.removeKeyValues args redditKeys

                let sort = keyValues |> Map.tryFind "sort" |?? "hot"
                let maybeFlair = keyValues |> Map.tryFind "flair"

                match args |> Seq.tryHead with
                | None -> return Message "No subreddit specified"
                | Some subreddit ->
                    match!
                        tokenStore.GetToken TokenType.Reddit
                        |-> Result.fromOption "Couldn't retrieve access token for Reddit API"
                        |> Result.bindAsync (fun token -> getPosts subreddit sort token)
                        |-> Result.map (fun r -> r.Data.Children)
                    with
                    | Error err -> return Message err
                    | Ok posts ->
                        match
                            posts
                            |> List.filter postFilter
                            |> fun ps -> maybeFlair |> Option.map (fun f -> ps |> List.filter (fun p -> flairFilter f p)) |?? ps
                        with
                        | [] -> return Message "No posts found!"
                        | filteredPosts ->
                            let post = filteredPosts |> List.randomChoice |> _.Data

                            let subreddit, url =
                                post.CrosspostParentList
                                |> Option.bind List.tryHead
                                |> Option.map (fun cp -> $"r/{post.Subreddit} (x-posted from r/{cp.Subreddit}", System.Web.HttpUtility.HtmlDecode(cp.Url))
                                |?? ($"r/{post.Subreddit}", System.Web.HttpUtility.HtmlDecode(post.Url))

                            let title = (System.Web.HttpUtility.HtmlDecode post.Title).Replace("\n", "")

                            return Message $"{subreddit} \"{title}\" (+{post.Score}) {url}"
        }
