namespace Commands.Api

module News =

    open Database

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    open System
    open System.ServiceModel.Syndication

    let private cache =
        new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime * SyndicationFeed>()

    let private getFromXmlAsync<'a> url =
        async {
            use! response =
                http {
                    GET url
                    Accept "text/xml"
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                use! stream = response.content.ReadAsStreamAsync() |> Async.AwaitTask
                let reader = new Xml.XmlTextReader(stream)
                let formatter = Rss20FeedFormatter()
                formatter.ReadFrom(reader)

                return Ok formatter.Feed
            | Error err -> return Error $"News RSS feed HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let private getRandomItem (feed: SyndicationFeed) = feed.Items |> Seq.randomChoice

    let getNews category =
        async {
            let category =
                match category with
                | None -> "World"
                | Some c -> c

            match! RssFeedRepository.get category with
            | DatabaseResult.Failure -> return Error "Error occured trying to get RSS feeds"
            | DatabaseResult.Success feeds ->

                let feed = feeds |> List.randomChoice
                let url = feed.Urls |> List.randomChoice

                match cache.TryGetValue url with
                | true, (updated, feed) when DateTime.UtcNow - updated < (feed.TimeToLive |? TimeSpan.FromMinutes(10L)) ->
                    return Ok(getRandomItem feed)
                | _, _ ->
                    match! getFromXmlAsync url with
                    | Error err -> return Error err
                    | Ok feed ->
                        cache[url] <- DateTime.UtcNow, feed
                        return Ok(getRandomItem feed)
        }
