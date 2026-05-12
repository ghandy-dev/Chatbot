module Chatbot.Database.Types

type UserId = int
type ChannelId = int
type ReminderId = int

type Alias =
    {
        Name: string
        Command: string
    }

type NewAlias =
    {
        UserId: UserId
        Name: string
        Command: string
    }

    static member create userId name command =
        {
            UserId = userId
            Name = name
            Command = command
        }

type UpdateAlias =
    {
        UserId: UserId
        Name: string
        Command: string
    }

    static member create userId name command =
        {
            UserId = userId
            Name = name
            Command = command
        }

type Channel =
    {
        ChannelId: ChannelId
        ChannelName: string
    }

type NewChannel =
    {
        ChannelId: ChannelId
        ChannelName: string
    }

    static member create channelId channelName =
        {
            ChannelId = channelId
            ChannelName = channelName
        }

type Reminder =
    {
        FromUsername: string
        TargetUsername: string
        Timestamp: System.DateTime
        Message: string
    }

and TimedReminder =
    {
        FromUsername: string
        TargetUsername: string
        Timestamp: System.DateTime
        Message: string
        Channel: string
    }

and NewReminder =
    {
        FromUserId: UserId
        FromUsername: string
        TargetUserId: UserId
        TargetUsername: string
        Channel: string option
        Message: string
        Timestamp: System.DateTime
        ReminderTimestamp: System.DateTime option
    }

    static member create fromUserId fromUsername targetUserId targetUsername channel message reminderTimestamp =
        {
            FromUserId = fromUserId
            FromUsername = fromUsername
            TargetUserId = targetUserId
            TargetUsername = targetUsername
            Channel = channel
            Message = message
            Timestamp = System.DateTime.UtcNow
            ReminderTimestamp = reminderTimestamp
        }

and UpdateReminder =
    {
        ReminderId: int
        UserId: UserId
        Message: string
    }

    static member create reminderId userId targetUserId message =
        {
            ReminderId = reminderId
            UserId = userId
            Message = message
        }


type RpsStats =
    {
        UserId: UserId
        Score: int
        TotalMoves: int
        Wins: int
        Losses: int
    }

    static member create userId =
        {
            UserId = userId
            Score = 0
            TotalMoves = 0
            Wins = 0
            Losses = 0
        }

    member this.addWin () =
        { this with
            Score = this.Score + 6
            TotalMoves = this.TotalMoves + 1
            Wins = this.Wins + 1
        }

    member this.addLoss () =
        { this with
            TotalMoves = this.TotalMoves + 1
            Losses = this.Losses + 1
        }

    member this.addDraw () =
        { this with
            Score = this.Score + 3
            TotalMoves = this.TotalMoves + 1
        }

type User =
    {
        UserId: UserId
        Username: string
        IsAdmin: bool
    }

    static member create userId username =
        {
            UserId = userId
            Username = username
            IsAdmin = false
        }

type NewUser =
    {
        UserId: UserId
        Username: string
        IsAdmin: bool
    }

    static member create userId username =
        {
            UserId = userId
            Username = username
            IsAdmin = false
        }

type IAliasService =
    abstract member Get: UserId -> aliasName: string -> Async<Alias option>
    abstract member Delete: aliasName: string -> UserId -> Async<Result<int, exn>>
    abstract member Add: NewAlias -> Async<Result<int, exn>>
    abstract member Update: UpdateAlias -> Async<Result<int, exn>>

type IChannelsService =
    abstract member Get: ChannelId -> Async<Channel option>
    abstract member GetAll: unit -> Async<Channel seq>
    abstract member Delete: ChannelId -> Async<Result<int, exn>>
    abstract member Add: NewChannel -> Async<Result<int, exn>>

type INewsFeedService =
    abstract member Get: category: string -> Async<string list>

type IReminderService =
    abstract member GetTimedReminders: unit -> Async<TimedReminder seq>
    abstract member GetReminders: UserId -> Async<Reminder seq>
    abstract member GetPendingTimedReminderCount: UserId -> Async<Result<int, exn>>
    abstract member GetPendingRemindersCount: UserId -> Async<Result<int, exn>>
    abstract member Add: NewReminder -> Async<Result<int, exn>>
    abstract member Update: UpdateReminder -> Async<Result<int, exn>>
    abstract member Delete: ReminderId -> Async<Result<int, exn>>

type IUsersService =
    abstract member Get: UserId -> Async<User option>
    abstract member Add: NewUser -> Async<Result<int, exn>>

type IRockPaperScissorsService =
    abstract member Get: UserId -> Async<RpsStats option>
    abstract member Add: RpsStats -> Async<Result<int, exn>>
    abstract member Update: RpsStats -> Async<Result<int, exn>>
