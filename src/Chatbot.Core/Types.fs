[<AutoOpen>]
module Types

type RoomState = {
    Channel: string
    EmoteOnly: bool
    FollowersOnly: bool
    R9K: bool
    RoomId: string
    Slow: int
    SubsOnly: bool
    LastMessageSent: System.DateTime
} with

    static member create channel emoteOnly followersOnly r9k roomId slow subsOnly = {
        Channel = channel
        EmoteOnly = emoteOnly |?? false
        FollowersOnly = followersOnly |?? false
        R9K = r9k |?? false
        RoomId = roomId
        Slow = slow |?? 0
        SubsOnly = subsOnly |?? false
        LastMessageSent = System.DateTime.UtcNow
    }

type UserState = {
    Moderator: bool
    Subscriber: bool
} with

    static member create moderator subscriber = {
        Moderator = moderator
        Subscriber = subscriber
    }