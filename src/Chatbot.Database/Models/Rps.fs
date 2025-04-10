namespace Database.Models

type RpsStats = {
    UserId: int
    Score: int
    TotalMoves: int
    Wins: int
    Losses: int
} with

    static member create userId = {
        UserId = userId
        Score = 0
        TotalMoves = 0
        Wins = 0
        Losses = 0
    }

    member this.addWin () = {
        this with
            Score = this.Score + 6
            TotalMoves = this.TotalMoves + 1
            Wins = this.Wins + 1
    }

    member this.addLoss () = {
        this with
            TotalMoves = this.TotalMoves + 1
            Losses = this.Losses + 1
    }

    member this.addDraw () = {
        this with
            Score = this.Score + 3
            TotalMoves = this.TotalMoves + 1
    }