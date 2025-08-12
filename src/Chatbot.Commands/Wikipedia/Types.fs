namespace Wikipedia

module Types =

    open System.Text.Json.Serialization

    module Core =

        type Thumbnail = {
            Mimetype: string
            // Size: int option
            Width: int
            Height: int
            // Duration: float option
            Url: string
        }

        type Page = {
            Id: int
            Key: string
            Title: string
            Excerpt: string
            // [<JsonPropertyName("matched_title")>]
            // MatchedTitle: string option
            Description: string
            // Thumbnail: Thumbnail option
        }

        type Pages =
            { Pages: Page list }

    module Feed =

        type Article = {
            Type: string
            Title: string
            PageId: int
            [<JsonPropertyName("content_urls")>]
            ContentUrls: ContentUrls
            Extract: string
        }

        and ContentUrls = {
            Desktop: Urls
        }

        and Urls = {
            Page: string
        }

        type MostRead = {
            [<JsonConverter(typeof<NonCompliantDateTimeOffsetConverter>)>]
            Date: System.DateTimeOffset
            Articles: Article list
        }

        type News = {
            Links: Article list
            Story: string
        }

        type OnThisDay = {
            Text: string
            Pages: Article list
            Year: int
        }

        type DidYouKnow = {
            Html: string
            Text: string
        }

        type Feed = {
            [<JsonPropertyName("fta")>]
            FeaturedArticle: Article
            MostRead: MostRead
            News: News list
            OnThisDay: OnThisDay list
            [<JsonPropertyName("dyk")>]
            DidYouKnow: DidYouKnow list
        }