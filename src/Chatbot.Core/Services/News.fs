namespace Chatbot.Core.Services

module News =

    open System
    open System.IO
    open System.ServiceModel.Syndication

    open FsToolkit.ErrorHandling
    open FSharpPlus

    type INewsService =
        abstract member Get: category: string option -> Async<Result<SyndicationItem, string>>
        abstract member Search: query: string -> string -> Async<Result<SyndicationItem, string>>

    module NewsService =

        open Chatbot.Core
        open Chatbot.Core.Domain.Types
        open Chatbot.Core.Caching
        open Chatbot.Core.Types
        open Chatbot.Common

        let create env (newsRepo: INewsFeedRepository) =

            let apiUrl = "https://news.google.com/rss"

            let client = env.HttpClient

            let searchUrl query countryCode =
                UrlBuilder.buildUrl
                    $"{apiUrl}/search"
                    [ "q", query ; "gl", countryCode ]

            let cache = env.Cache

            let getFeed (url: string) =
                asyncResult {
                    let! response =
                        Http.Request.get url
                        |> Http.send client
                        |> Async.map Http.Response.toResult
                        |> AsyncResult.mapError (fun err -> $"Http Error: {err.StatusCode}")

                    try
                        use stream = new StringReader(response.Content)
                        use reader = new Xml.XmlTextReader(stream)
                        let formatter = Rss20FeedFormatter()
                        formatter.ReadFrom(reader)

                        return! formatter.Feed |> Option.ofNull |> Option.toResultWith "No feed data"
                    with ex ->
                        return! Error <| ex.Message
                }

            let getRssFeed (url: string) =
                let getRandomItem (feed: SyndicationFeed) = feed.Items |> Seq.randomChoice

                async {
                    match cache |> MemoryCache.tryGetValue url with
                    | Some (updated, feed: SyndicationFeed) when DateTime.UtcNow - updated < (Option.ofNullable feed.TimeToLive |> Option.defaultValue (TimeSpan.FromMinutes(10L))) ->
                        return Ok(getRandomItem feed)
                    | _ ->
                        return!
                            getFeed url
                            |> AsyncResult.tee (fun feed -> env.Cache |> MemoryCache.set url (DateTime.UtcNow, feed) |> ignore)
                            |> AsyncResult.map getRandomItem
                }

            let getFromCategory categoryOpt =
                async {
                    let! categories = newsRepo.GetCategories ()

                    let category =
                        match categoryOpt with
                        | None -> "Top Stories"
                        | Some c ->
                            categories
                            |> List.tryFind ((=) c)
                            |> Option.defaultValue (categories |> List.randomChoice)

                    let! urls = newsRepo.Get category

                    match urls with
                    | [] -> return Error $"No news feeds for category {category}"
                    | urls ->
                        let url = urls |> List.randomChoice
                        return! getRssFeed url
                }

            let search query countryCode =
                asyncResult {
                    let url = searchUrl query countryCode
                    let! feed = getRssFeed url

                    feed.Title <- new TextSyndicationContent(feed.Title.Text.Replace($" - {feed.SourceFeed.Title.Text}", ""))
                    feed.Summary <- new TextSyndicationContent(feed.Title.Text.Replace($" - {feed.SourceFeed.Title.Text}", ""))

                    if feed.Title.Text = feed.Summary.Text then
                        feed.Summary <- null

                    return feed
                }

            {
                new INewsService with
                    member _.Get category = getFromCategory category
                    member _.Search query countryCode = search query countryCode
            }