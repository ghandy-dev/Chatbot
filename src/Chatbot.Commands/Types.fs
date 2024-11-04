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

    static member create channel emoteOnly followersOnly r9k roomId slow subsOnly = {
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

type Source =
    | Whisper of username: string
    | Channel of channel: RoomState

type Emotes = {
    GlobalEmotes: Emotes.Emote list
    ChannelEmotes: Emotes.Emote list
} with

    member this.TryFind (emote: string) =
        this.GlobalEmotes |> List.tryFind (fun e -> e.Name = emote) |> Option.orElseWith (fun _ -> this.ChannelEmotes |> List.tryFind (fun e -> e.Name = emote))

    member this.Random () =
        match this.GlobalEmotes, this.ChannelEmotes with
        | [], [] -> None
        | g, [] -> g |> List.tryRandomChoice
        | [], c -> c |> List.tryRandomChoice
        | g, c ->
            [ g ; c ]
            |> List.randomChoice
            |> function
                | e -> e |> List.tryRandomChoice

    member this.Random provider =
        match
            this.GlobalEmotes |> List.filter (fun e -> e.Provider = provider),
            this.ChannelEmotes |> List.filter (fun e -> e.Provider = provider)
        with
        | [], [] -> None
        | g, [] -> g |> List.tryRandomChoice
        | [], c -> c |> List.tryRandomChoice
        | g, c ->
            [ g ; c ]
            |> List.randomChoice
            |> function
                | e -> e |> List.tryRandomChoice


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
    | RefreshChannelEmotes of channelId: string
    | RefreshGlobalEmotes of emoteProvider: Emotes.EmoteProvider

type CommandResult =
    | Message of string
    | RunAlias of command: string * args: string list
    | Pipe of string list
    | BotAction of BotCommand * string

type Args = string list

type CommandFunction =
    | S of (unit -> CommandResult) // SyncFunction
    | SA of (Args -> CommandResult) // SyncFunctionWithArgs
    | SAC of (Args -> Context -> CommandResult) // SyncFunctionWithArgsAndContext
    | A of (unit -> Async<CommandResult>) // AsyncFunction
    | AA of (Args -> Async<CommandResult>) // AsyncFunctionWithArgs
    | AAC of (Args -> Context -> Async<CommandResult>) // AsyncFunctionWithArgsAndContext
    | AACM of (Args -> Context -> Map<string, Command> -> Async<CommandResult>) // AsyncFunctionWithArgsAndContextAndCommands

and Command = {
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

and Details = {
    Name: string
    Description: string
    ExampleUsage: string
}
