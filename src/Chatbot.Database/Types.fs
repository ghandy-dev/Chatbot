namespace Chatbot.Database

module Types =

    module Users =

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

    module RockPaperScissors =

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

    module Channels =

        type Channel = {
            ChannelId: string
            ChannelName: string
        } with

            static member create channelId channelName = {
                ChannelId = channelId
                ChannelName = channelName
            }

    module Aliases =

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

    module News =

        type RssFeeds = {
            Urls: string list
            Category: string
        }

    module Dungeon =

        type DungeonPlayer = {
            UserId: int
            Data: string
        } with

            static member create userId data = {
                UserId = userId
                Data = data
            }

    module Reminders =

        type Reminder = {
            FromUsername: string
            TargetUsername: string
            Timestamp: System.DateTime
            Message: string
        }

        and TimedReminder = {
            FromUsername: string
            TargetUsername: string
            Timestamp: System.DateTime
            Message: string
            Channel: string
        }

        and CreateReminder = {
            FromUserId: int
            FromUsername: string
            TargetUserId: int
            TargetUsername: string
            Channel: string option
            Message: string
            Timestamp: System.DateTime
            ReminderTimestamp: System.DateTime option
        } with

            static member Create fromUserId fromUsername targetUserId targetUsername channel message reminderTimestamp = {
                FromUserId = fromUserId
                FromUsername = fromUsername
                TargetUserId = targetUserId
                TargetUsername = targetUsername
                Channel = channel
                Message = message
                Timestamp = System.DateTime.UtcNow
                ReminderTimestamp = reminderTimestamp
            }

        and UpdateReminder = {
            ReminderId: int
            UserId: int
            Message: string
        } with

            static member Create reminderId userId targetUserId message = {
                ReminderId = reminderId
                UserId = userId
                Message = message
            }