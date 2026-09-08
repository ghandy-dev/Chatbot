namespace Chatbot.Infrastructure.Services.Ivr.Types

open System

type Emote = {
    ChannelName: string option
    ChannelLogin: string option
    ChannelId: string option
    Artist: Artist option
    EmoteId: string
    EmoteCode: string
    EmoteUrl: string
    EmoteSetId: string option
    EmoteAssetType: string option
    EmoteState: string
    EmoteType: string
    EmoteTier: string option
}

and Artist = {
    DisplayName: string
    Login: string
    Id: string
}

type SubAge = {
    User: User
    Channel: Channel
    StatusHidden: bool option
    FollowedAt: DateTimeOffset option
    Streak: Stats option
    Cumulative: Stats option
    Meta: SubMeta option
}

and User = {
    Id: string
    Login: string
    DisplayName: string
}

and Channel = {
    Id: string
    Login: string
    DisplayName: string
}

and Stats = {
    ElapsedDays: int
    DaysRemaining: int
    Months: int
    End: DateTimeOffset
    Start: DateTimeOffset
}

and SubMeta = {
    Type: string
    Tier: string
    EndsAt: DateTimeOffset option
    RenewsAt: DateTimeOffset option
    GiftMeta: GiftMeta option
}

and GiftMeta = {
    GiftDate: DateTimeOffset
    Gifter: Gifter
}

and Gifter = {
    Id: string
    Login: string
    DisplayName: string
}
