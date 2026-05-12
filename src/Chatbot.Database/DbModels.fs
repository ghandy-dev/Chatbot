module Chatbot.Database.DbModels

type DbAlias =
    {
        alias_id: int
        user_id: int
        name: string
        command: string
    }

type DbChannel =
    {
        channel_id: int
        channel_name: string
    }

type DbNewsFeed =
    {
        rss_feed_id: int
        category_id: int
        url: string
    }

type DbNewsFeedCategory =
    {
        category_id: int
        category: string
    }

type DbReminder =
    {
        reminder_id: int
        timestamp: string
        from_user_id: int
        from_username: string
        target_user_id: int
        target_username: string
        message: string
        reminded: int
    }

type DbTimedReminder =
    {
        reminder_id: int
        timestamp: string
        from_user_id: int
        from_username: string
        target_user_id: int
        target_username: string
        message: string
        channel: string
        reminder_timestamp: string
        reminded: int
    }

type DbRpsStats =
    {
        rps_stats_id: int64
        user_id: int
        score: int
        total_moves: int
        wins: int
        losses: int
    }

type DbUser =
    {
        user_id: int
        username: string
        is_admin: bool
    }
