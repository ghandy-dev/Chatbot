namespace Chatbot.Database

module Types =

    type User = {
        UserId: int
        Username: string
        IsAdmin: bool
    } with

        static member create userId username = {
            UserId = userId
            Username = username
            IsAdmin = false
        }

    type RpsStats = {
        UserId: int
        Score: int
        TotalMoves: int
        Wins: int
        Losses: int
    } with

        static member newStats userId = {
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

    type Channel = {
        ChannelId: string
        ChannelName: string
    } with

        static member create channelId channelName = {
            ChannelId = channelId
            ChannelName = channelName
        }

    type Alias = {
        UserId: int
        Name: string
        Command: string
    } with

        static member create userId name command = {
            UserId = userId
            Name = name
            Command = command
        }

    type RssFeeds = {
        Urls: string list
        Category: string
    }

    type DungeonPlayer = {
        UserId: int
        Data: string
    } with

        static member create userId data = {
            UserId = userId
            Data = data
        }
