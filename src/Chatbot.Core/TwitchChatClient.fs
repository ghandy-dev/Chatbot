namespace Clients

open IRC

open System

module Rates =

    let [<Literal>] MessageLimit_Chat = 20 // per 30 seconds
    let [<Literal>] Interval_Chat = 30s

    let [<Literal>] MessageLimit_Whispers = 100
    let [<Literal>] Interval_Whispers = 60s

type RateLimiter(messagesPerInterval, interval: int16) =

    let interval = interval |> int
    let mutable messageCount = 0
    let mutable lastMessageTimestamp = DateTime.UtcNow

    member private _.TimeSinceLastReset =
        (DateTime.UtcNow - lastMessageTimestamp).TotalSeconds |> int

    member _.MessageCount
        with get () = messageCount
        and set (value) = messageCount <- value

    member _.LastReset
        with get () = lastMessageTimestamp
        and set (value) = lastMessageTimestamp <- value


    member this.TimeUntilReset = this.TimeSinceLastReset - interval

    member this.CanSend () =
        if this.TimeSinceLastReset > interval then
            messageCount <- 1
            lastMessageTimestamp <- DateTime.Now
            true
        elif this.MessageCount < messagesPerInterval then
            messageCount <- messageCount + 1
            lastMessageTimestamp <- DateTime.Now
            true
        else
            false

[<RequireQualifiedAccess>]
type ConnectionType =
    | IRC of Host: string * Port: int
    | Websocket of Host: string * Port: int

type TwitchChatClientConfig = {
    UserId: string
    Username: string
    Capabilities: string array
}

type TwitchChatClient(Connection: ConnectionType, Config: TwitchChatClientConfig) =

    let lastMessagesSent = new Collections.Concurrent.ConcurrentDictionary<string, int64>()
    let messageReceived = new Event<Messages.Types.IrcMessageType array>()
    let chatRateLimiter = RateLimiter(Rates.MessageLimit_Chat, Rates.Interval_Chat)
    let twitchService = Services.services.TwitchService

    let whisperRateLimiter =
        RateLimiter(Rates.MessageLimit_Whispers, Rates.Interval_Whispers)

    let createClient () : ITwitchConnection =
        match Connection with
        | ConnectionType.IRC(host, port) -> new IrcClient(host, port)
        | ConnectionType.Websocket(host, port) -> new WebSocketClient(host, port)

    let mutable client: ITwitchConnection = null

    let authenticate user =
        async {
            match! Authorization.tokenStore.GetToken Authorization.TokenType.Twitch with
            | None -> failwithf "Couldn't retrieve access token for Twitch API"
            | Some token -> do! client.AuthenticateAsync(user, token, Config.Capabilities)
        }

    let send (command: IRC.Command) =
        async {
            let ircMessage = command.ToString()

            match command with
            | IRC.PrivMsg (channel, _) ->
                let now = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                if chatRateLimiter.CanSend() then
                    match lastMessagesSent |> Dictionary.tryGetValue channel with
                    | None ->
                        lastMessagesSent[channel] <- now
                        do! client.SendAsync ircMessage
                    | Some timestamp ->
                        let elapsedMs = now - timestamp |> int
                        if elapsedMs > 1000 then
                            lastMessagesSent[channel] <- now
                            do! client.SendAsync ircMessage
                        else
                            do! Async.Sleep(1000 - elapsedMs)
                            lastMessagesSent[channel] <- now
                            do! client.SendAsync ircMessage
            | _ -> do! client.SendAsync ircMessage
        }

    let sendWhisper toUserId message accessToken =
        async {
            if whisperRateLimiter.CanSend() then
                do! twitchService.SendWhisper Config.UserId toUserId message accessToken |> Async.Ignore
        }

    let reader (cancellationToken) =
        async {
            let rec reader' () =
                async {
                    match client.Connected with
                    | true ->
                        match! client.ReadAsync cancellationToken with
                        | Some message ->
                            message |> fun m -> m.Split([| '\r' ; '\n' |], StringSplitOptions.RemoveEmptyEntries) |> Array.iter Logging.info
                            let parsedMessage = message |> Parsing.Parser.parseIrcMessage |> Array.map Messages.MessageMapping.mapIrcMessage |> Array.choose id
                            messageReceived.Trigger parsedMessage
                            do! reader' ()
                        | None -> () // if this happens then the client isn't connected
                    | false -> Logging.warning "IRC client disconnected."
                }

            do! reader' ()
        }

    let start (cancellationToken) =
        async {
            client <- createClient ()
            do! client.ConnectAsync (cancellationToken)
            do! authenticate Config.Username

            let! result = reader cancellationToken |> Async.StartChild |> Async.Catch

            match result with
            | Choice1Of2 _ -> Logging.trace "Reader started."
            | Choice2Of2 ex -> Logging.error $"Exception occurred in {nameof (reader)}" ex
        }

    let reconnect (cancellationToken) =
        async {
            (client :> IDisposable).Dispose()
            do! start cancellationToken
        }

    [<CLIEvent>]
    member _.MessageReceived = messageReceived.Publish

    member _.StartAsync (cancellationToken: Threading.CancellationToken) = start (cancellationToken)

    member _.SendAsync (message: IRC.Command) = send message

    member _.WhisperAsync (toUserId, message, accessToken) = sendWhisper toUserId message accessToken

    member _.ReconnectAsync (cancellationToken) = reconnect (cancellationToken)
