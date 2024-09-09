namespace Chatbot.Commands.Reddit

[<AutoOpen>]
module Reddit =

    open Api
    open Chatbot.Authorization
    open Chatbot.Commands

    let redditKeys = [ "sort" ; "flair" ]

    let private postFilter (p: Types.Thing<Types.T3>) =
        not <| p.Data.Over18 && not <| p.Data.IsSelf

    let private flairFilter (flair: string) (p: Types.Thing<Types.T3>) =
        p.Data.Flair |?? "" |> fun f -> System.String.Compare(f, flair, true) = 0

    let reddit (args: string list) =
        async {
            match args with
            | [] -> return Message "No subreddit specified"
            | subreddit :: a ->
                let keyValues = KeyValueParser.parse a redditKeys
                let sort = keyValues |> Map.tryFind "sort" |?? "hot"
                let flair = keyValues |> Map.tryFind "flair"

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
                        |> fun ps -> flair |> Option.map (fun f -> ps |> List.filter (fun p -> flairFilter f p)) |?? ps
                    with
                    | [] -> return Message "No posts found!"
                    | filteredPosts ->
                        let post = filteredPosts |> List.randomChoice |> _.Data

                        let subreddit, url =
                            post.CrosspostParentList |?? []
                            |> List.tryHead
                            |> Option.map (fun cp -> $"r/{post.Subreddit} (x-posted from r/{cp.Subreddit}", System.Web.HttpUtility.HtmlDecode(cp.Url))
                            |?? ($"r/{post.Subreddit}", System.Web.HttpUtility.HtmlDecode(post.Url))

                        let title = (System.Web.HttpUtility.HtmlDecode post.Title).Replace("\n", "")

                        return Message $"{subreddit} \"{title}\" (+{post.Score}) {url}"
        }
