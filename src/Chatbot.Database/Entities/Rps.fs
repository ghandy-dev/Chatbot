namespace Database.Entities

type RpsStats = {
    rps_stats_id: int64
    user_id: int
    score: int
    total_moves: int
    wins: int
    losses: int
}
