namespace Chatbot.Database

[<RequireQualifiedAccess>]
module Entities =

    type internal User = {
        user_id: int
        username: string
        is_admin: bool
    }

    type internal RpsStats = {
        rps_stats_id: int64
        user_id: int
        score: int
        total_moves: int
        wins: int
        losses: int
    }

    type internal Channel = {
        channel_id: int
        channel_name: string
    }

    type internal Alias = {
        alias_id: int
        user_id: int
        name: string
        command: string
    }

    type internal NewsFeed = {
        rss_feed_id: int
        category_id: int
        url: string
    }

    type internal NewsFeedCategory = {
        category_id: int
        category: string
    }

    type internal DungeonPlayer = {
        user_id: int
        data: string
    }