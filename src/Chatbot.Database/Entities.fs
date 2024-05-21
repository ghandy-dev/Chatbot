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
