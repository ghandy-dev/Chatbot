namespace Chatbot.Core.Services


module News =

    open System
    open System.ServiceModel.Syndication

    open FsToolkit.ErrorHandling

    type INewsService =
        abstract member Get: category: string option -> Async<Result<SyndicationItem, string>>
        abstract member Search: query: string -> string -> Async<Result<SyndicationItem, string>>

    module NewsService =

        open Chatbot.Core.Domain.Types
        open Chatbot.Core.Caching
        open Chatbot.Core.Types
        open Chatbot.Common

        let create env (newsRepo: INewsFeedRepository) =

            let apiUrl = "https://news.google.com/rss"

            let searchUrl query countryCode =
                UrlBuilder.buildUrl
                    $"{apiUrl}/search"
                    [ "q", query ; "gl", countryCode ]

            let cache = env.Cache

            let tryGetFeed (url: string) =
                async {
                    use reader = new Xml.XmlTextReader(url)
                    let formatter = Rss20FeedFormatter()
                    formatter.ReadFrom(reader)

                    return formatter.Feed |> Option.ofNull
                }

            let getRssFeed (url: string) =
                let getRandomItem (feed: SyndicationFeed) = feed.Items |> Seq.randomChoice

                async {
                    match cache |> MemoryCache.tryGetValue url with
                    | Some (updated, feed: SyndicationFeed) when DateTime.UtcNow - updated < (Option.ofNullable feed.TimeToLive |> Option.defaultValue (TimeSpan.FromMinutes(10L))) ->
                        return Ok(getRandomItem feed)
                    | _ ->
                        match! tryGetFeed url with
                        | None -> return Error "Error reading RSS feed"
                        | Some feed ->
                            env.Cache |> MemoryCache.set url (DateTime.UtcNow, feed) |> ignore
                            return Ok(getRandomItem feed)

                }

            let getFromCategory categoryOpt =
                async {
                    let! categories = newsRepo.GetCategories ()

                    let category =
                        match categoryOpt with
                        | None -> "Top Stories"
                        | Some c ->
                            if categories |> List.contains c then
                                c
                            else
                                categories |> List.randomChoice

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