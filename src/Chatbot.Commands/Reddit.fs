namespace Chatbot.Commands

[<AutoOpen>]
module Reddit =

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain.Commands.CommandError
    open Chatbot.Core.Services.Reddit
    open Chatbot.Core.Services.Reddit.Types

    let private postFilter (p: Thing<T3>) =
        not <| p.Data.Over18 && not <| p.Data.IsSelf

    let private flairFilter (flair: string) (p: Thing<T3>) =
        (p.Data.Flair |? "", flair) ||> strCompareIgnoreCase

    let private redditKeys = [ "sort" ; "flair" ]
    let private defaultSorting = "hot"
    let private sortings = [ "hot" ; "top" ; "best" ]

    let reddit (redditService: RedditService) context =
        asyncResult {
            match context.MessageArgs with
            | [] -> return! invalidArgs "No subreddit specified"
            | args ->
                let kvp = KeyValueParser.parse args redditKeys

                let! sort =
                    match kvp.KeyValues.TryFind "sort" with
                    | None -> Some defaultSorting
                    | Some s -> sortings |> List.tryFind ((=) s)
                    |> Result.requireSome (InvalidArgs $"""Unknown sorting. Valid sortings: {sortings |> strJoin ", "}""")

                let maybeFlair = kvp.KeyValues.TryFind "flair"

                let! subreddit = kvp.Input |> Seq.tryHead |> Option.toResultWith (InvalidArgs "No subreddit specified")

                let! response = redditService.GetPosts subreddit sort |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Reddit")

                let posts =
                    response.Data.Children |> List.filter postFilter
                    |> fun ps ->
                        match maybeFlair with
                        | None -> ps
                        | Some flair -> ps |> List.filter (flairFilter flair)

                match posts with
                | [] -> return [ Message "No posts found!" ]
                | filteredPosts ->
                    let post = filteredPosts |> List.randomChoice |> _.Data

                    let subreddit, url =
                        post.CrosspostParentList
                        |> Option.bind List.tryHead
                        |> Option.map (fun cp -> $"r/{post.Subreddit} (x-posted from r/{cp.Subreddit}", htmlDecode cp.Url)
                        |? ($"r/{post.Subreddit}", htmlDecode post.Url)

                    let title = (htmlDecode post.Title).Replace("\n", "")

                    return [ Message $"{subreddit} \"{title}\" (+{post.Score}) {url}" ]
        }
