namespace Chatbot.Core.Services


module News =

    open System
    open System.ServiceModel.Syndication

    open FsToolkit.ErrorHandling

    type INewsService =
        abstract member GetNews: category: string option -> Async<Result<SyndicationItem, string>>

    module NewsService =

        open Chatbot.Core.Domain.Types
        open Chatbot.Core.Caching
        open Chatbot.Core.Types

        let create env (newsRepo: INewsFeedRepository) =
            let cache = env.Cache

            let tryGetFeed (url: string) =
                async {
                    use reader = new Xml.XmlTextReader(url)
                    let formatter = Rss20FeedFormatter()
                    formatter.ReadFrom(reader)

                    return formatter.Feed |> Option.ofNull
                }

            let getRandomItem (feed: SyndicationFeed) = feed.Items |> Seq.randomChoice

            let getNews category =
                async {
                    let category =
                        match category with
                        | None -> "World"
                        | Some c -> c

                    let! urls = newsRepo.Get category

                    let url = urls |> List.randomChoice

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

            {
                new INewsService with
                    member _.GetNews category = getNews category
            }