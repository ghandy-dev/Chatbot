namespace News

module Api =

    open System
    open System.Net.Http
    open System.ServiceModel.Syndication

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open Commands
    open Database
    open Commands.CommandError

    let private cache =
        new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime * SyndicationFeed>()

    let private tryGetFeed (url: string) =
        async {
            use reader = new Xml.XmlTextReader(url)
            let formatter = Rss20FeedFormatter()
            formatter.ReadFrom(reader)

            return formatter.Feed |> Option.ofNull
        }

    let private getRandomItem (feed: SyndicationFeed) = feed.Items |> Seq.randomChoice

    let getNews category =
        async {
            let category =
                match category with
                | None -> "World"
                | Some c -> c

            match! NewsFeedRepository.get category with
            | DatabaseResult.Failure -> return internalError "Error occured trying to get RSS feeds"
            | DatabaseResult.Success urls ->
                let url = urls |> List.randomChoice

                match cache |> Dict.tryGetValue url with
                | Some (updated, feed) when DateTime.UtcNow - updated < (Option.ofNullable feed.TimeToLive |> Option.defaultValue (TimeSpan.FromMinutes(10L))) ->
                    return Ok(getRandomItem feed)
                | _ ->
                    match! tryGetFeed url with
                    | None -> return internalError "Error reading RSS feed"
                    | Some feed ->
                        cache[url] <- DateTime.UtcNow, feed
                        return Ok(getRandomItem feed)
        }
