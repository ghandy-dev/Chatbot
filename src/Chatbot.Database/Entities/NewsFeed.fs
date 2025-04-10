namespace Database.Entities

type NewsFeed = {
    rss_feed_id: int
    category_id: int
    url: string
}

type NewsFeedCategory = {
    category_id: int
    category: string
}
