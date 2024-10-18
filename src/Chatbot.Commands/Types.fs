namespace Commands

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

    static member create (channel, emoteOnly, followersOnly, r9k, roomId, slow, subsOnly) = {
        Channel = channel
        EmoteOnly = emoteOnly |?? false
        FollowersOnly = followersOnly |?? false
        R9K = r9k |?? false
        RoomId = roomId
        Slow = slow |?? 0
        SubsOnly = subsOnly |?? false
        LastMessageSent = System.DateTime.Now
    }

type RoomStates = Map<string, RoomState>

type Source = Whisper of username: string | Channel of channel: RoomState

type Emotes = {
    GlobalEmotes: Emotes.Emotes
    ChannelEmotes: Emotes.Emotes option
} with

    member this.Find emote =
        this.GlobalEmotes.Find emote
        |> Option.orElseWith (fun _ ->
            this.ChannelEmotes |> Option.bind (fun ce -> ce.Find emote)
        )

    member this.Random () =
        match this.ChannelEmotes with
        | Some ce when ce.Count () > 0 ->
            System.Random.Shared.Next(0, 2)
            |> function
            | 0 -> this.GlobalEmotes.Random ()
            | 1 -> ce.Random ()
            | _ -> failwith "Expect random between 0 and 1 (inclusive)"
        | _ -> this.GlobalEmotes.Random ()

type Context = {
    UserId: string
    Username: string
    IsAdmin: bool
    Source: Source
    Emotes: Emotes
} with

    static member createContext id username admin source emotes = {
        UserId = id
        Username = username
        IsAdmin = admin
        Source = source
        Emotes = emotes
    }

type BotCommand =
    | JoinChannel of channel: string * channelId: string
    | LeaveChannel of channel: string
    | RefreshChannelEmotes of channelId: string * emoteProvider: Emotes.EmoteProvider
    | RefreshGlobalEmotes of emoteProvider: Emotes.EmoteProvider

type CommandResult =
    | Message of string
    | RunAlias of command: string * args: string list
    | Pipe of string list
    | BotAction of BotCommand * string

type Parameters = string list

type CommandFunction =
    | SyncFunction of (unit -> CommandResult)
    | SyncFunctionWithArgs of (Parameters -> CommandResult)
    | SyncFunctionWithArgsAndContext of (Parameters -> Context -> CommandResult)
    | AsyncFunction of (unit -> Async<CommandResult>)
    | AsyncFunctionWithArgs of (Parameters -> Async<CommandResult>)
    | AsyncFunctionWithArgsAndContext of (Parameters -> Context -> Async<CommandResult>)

type Details = {
    Name: string
    Description: string
    ExampleUsage: string
}

type Command = {
    Name: string
    Aliases: string list
    Details: Details
    Execute: CommandFunction
    Cooldown: int
    AdminOnly: bool
} with

    static member createCommand (name, alias, description, func, cooldown, adminOnly) = {
        Name = name
        Aliases = alias
        Details = description
        Execute = func
        Cooldown = cooldown
        AdminOnly = adminOnly
    }
