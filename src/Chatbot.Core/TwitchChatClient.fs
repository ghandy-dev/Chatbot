namespace Clients

open System

open FSharpPlus

open IRC
open IRC.Commands
open RateLimiter

[<RequireQualifiedAccess>]
type ConnectionType =
    | IRC of Host: string * Port: int
    | Websocket of Host: string * Port: int

type TwitchChatClientConfig = {
    UserId: string
    Username: string
    Capabilities: string array
}

type TwitchChatClient(connection: ConnectionType, config: TwitchChatClientConfig) =
    let lastMessagesSent = new Collections.Concurrent.ConcurrentDictionary<string, int64>()
    let messageReceived = new Event<Messages.Types.IrcMessageType array>()
    let chatRateLimiter = RateLimiter(Rates.MessageLimit_Chat, Rates.Interval_Chat)
    let whisperRateLimiter = RateLimiter(Rates.MessageLimit_Whispers, Rates.Interval_Whispers)
    let twitchService = Services.services.TwitchService

    let mutable client: ITwitchConnection = null

    let createClient () : ITwitchConnection =
        match connection with
        | ConnectionType.IRC(host, port) -> new IrcClient(host, port)
        | ConnectionType.Websocket(host, port) -> new WebSocketClient(host, port)

    let authenticate user cancellationToken =
        async {
            match! Authorization.tokenStore.GetToken Authorization.TokenType.Twitch with
            | Error _ -> failwithf "Couldn't retrieve access token for Twitch API"
            | Ok token -> do! client.AuthenticateAsync(user, token, config.Capabilities, cancellationToken)
        }

    let [<Literal>] GlobalSlow = 1200

    let send command cancellationToken =
        let sendPrivMsg (message: string) (channel: string) =
            async {
                Logging.info $"Sending: %s{message}"
                do! client.SendAsync(message, cancellationToken)
                lastMessagesSent[channel] <- DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }

        async {
            let ircMessage = command.ToString()

            match command with
            | PrivMsg (channel, _) ->
                let now = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                if chatRateLimiter.CanSend() then
                    match lastMessagesSent |> Dict.tryGetValue channel with
                    | None -> do! sendPrivMsg ircMessage channel
                    | Some timestamp ->
                        let elapsedMs = now - timestamp |> int
                        if elapsedMs > GlobalSlow then
                            do! sendPrivMsg ircMessage channel
                        else
                            do! Async.Sleep(GlobalSlow - elapsedMs)
                            do! sendPrivMsg ircMessage channel
            | _ ->
                Logging.info $"Sending: %s{ircMessage}"
                do! client.SendAsync (ircMessage, cancellationToken)
        }

    let sendWhisper toUserId message accessToken =
        async {
            if whisperRateLimiter.CanSend() then
                do! twitchService.SendWhisper config.UserId toUserId message accessToken |> Async.Ignore
        }

    let reader (cancellationToken: Threading.CancellationToken) =
        let splitMessages (message: string) = message.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
        let logMessages = Array.iter Logging.info
        let parseMessages = Parsing.Parser.parseIrcMessages >> Array.map Messages.MessageMapping.mapIrcMessage >> Array.choose id

        let rec reader' () =
            async {
                match client.Connected with
                | true ->
                    match! client.ReadAsync cancellationToken with
                    | Some message ->
                        let messages = splitMessages message
                        logMessages messages
                        messageReceived.Trigger (parseMessages messages)
                        do! reader' ()
                    | None -> ()
                | false -> Logging.warning "Twitch chat client disconnected."
            }

        reader' ()

    let start (cancellationToken) =
        async {
            client <- createClient ()
            do! client.ConnectAsync (cancellationToken)
            do! authenticate config.Username cancellationToken

            let! result = reader cancellationToken |> Async.StartChild |> Async.Catch

            match result with
            | Choice1Of2 _ -> Logging.trace "Reader started."
            | Choice2Of2 ex -> Logging.error $"Exception occurred in {nameof reader}" ex
        }

    let reconnect (cancellationToken) =
        async {
            (client :> IDisposable).Dispose()
            do! start cancellationToken
        }

    [<CLIEvent>]
    member _.MessageReceived = messageReceived.Publish

    member _.StartAsync (cancellationToken) = start cancellationToken

    member _.SendAsync (message, cancellationToken) = send message cancellationToken

    member _.WhisperAsync (toUserId, message, accessToken) = sendWhisper toUserId message accessToken

    member _.ReconnectAsync (cancellationToken) = reconnect cancellationToken
