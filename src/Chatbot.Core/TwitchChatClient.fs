namespace Clients

open System

open IRC
open RateLimiter

[<RequireQualifiedAccess>]
type ConnectionType =
    | IRC of Host: string * Port: int
    | Websocket of Host: string * Port: int

type TwitchChatClientConfig = {
    UserId: string
    Username: string
    Capabilities: string seq
    Channels: string seq
}

type TwitchClient = {
    Start: unit -> unit
    Reconnect: unit -> unit
    Send : IRC.Request -> unit
    SendWhisper: string -> string -> string -> unit
    MessageReceived: IEvent<IrcMessage>
}

module Twitch =

    open System.Threading

    // Actually 1000ms, but some extra time to allow for latency variation
    let [<Literal>] GlobalSlow = 1200

    type private Request =
        | Start
        | SendIrc of IRC.Request
        | SendWhisper of userId: string * message: string * accessToken: string
        | Reconnect of attempt: int

    let private createConnection connection : IConnection =
        match connection with
        | ConnectionType.IRC(host, port) -> new IrcClient(host, port)
        | ConnectionType.Websocket(host, port) -> new WebSocketClient(host, port)

    let createClient (connection: ConnectionType) (config: TwitchChatClientConfig) (cancellationToken: System.Threading.CancellationToken) =
        let chatRateLimiter = RateLimiter(Rates.MessageLimit_Chat, Rates.Interval_Chat)
        let whisperRateLimiter = RateLimiter(Rates.MessageLimit_Whispers, Rates.Interval_Whispers)
        let twitchService = Services.services.TwitchService
        let messageReceived = new Event<IrcMessage>()
        let mutable cancellationTokenSource: CancellationTokenSource option = None

        let agent = MailboxProcessor<Request>.Start(fun mb ->
            let authenticate (client: IConnection) =
                async {
                    match! Authorization.tokenStore.GetToken Authorization.TokenType.Twitch with
                    | Error _ -> failwithf "Couldn't retrieve access token for Twitch API"
                    | Ok token ->
                        do! client.SendAsync (Request.capReq config.Capabilities |> Request.toString, cancellationToken)
                        do! client.SendAsync (Request.pass token |> Request.toString, cancellationToken)
                        do! client.SendAsync (Request.nick config.Username |> Request.toString, cancellationToken)
                }

            let backoff attempt =
                match attempt with
                | 0 -> 1.0
                | 1 -> 4.0
                | 2 -> 16.0
                | 3 -> 64.0
                | _ -> 256.0
                |> TimeSpan.FromSeconds

            let createReader (client: IConnection) cancellationToken =
                let rec loop () =
                    async {
                        let splitMessages (message: string) = message.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                        let logMessages = Array.iter Logging.info
                        let parseMessages = Array.map (IRC.Parsing.parseRaw >> IRC.Messages.parseMessage) >> Array.choose id

                        if client.Connected then
                            match! client.ReadAsync cancellationToken with
                            | Some message ->
                                let messages =
                                    splitMessages message
                                    |> fun ms ->
                                        do logMessages ms
                                        parseMessages ms

                                messages
                                |> Seq.iter (fun msg ->
                                    match msg with
                                    | PingMessage msg -> do mb.Post (SendIrc <| Pong msg.Message)
                                    | ReconnectMessage ->
                                        Logging.info "Twitch servers requested we reconnect..."
                                        do mb.Post (Reconnect 0)
                                    | _ -> ()

                                    messageReceived.Trigger msg
                                )

                                return! loop ()
                            | None ->
                                mb.Post (Reconnect 0)
                                Logging.warning "Error reading message. Attempting to reconnect..."
                                return ()
                        else
                            Logging.warning "Twitch client disconnected."
                }

                loop ()

            let connect (client: IConnection) =
                async {
                    cancellationTokenSource |> Option.iter (fun cts -> cts.Cancel() ; cts.Dispose())

                    let cts = new CancellationTokenSource()
                    use linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken)
                    cancellationTokenSource <- Some cts

                    do! client.ConnectAsync(cancellationToken)
                    do! client |> authenticate
                    Async.Start (createReader client linkedCts.Token, linkedCts.Token)

                    do! client.SendAsync (Request.joinM config.Channels |> Request.toString, cancellationToken)
                    Logging.info "Twitch client connected."
                }

            let reconnect attempt (client: IConnection) =
                async {
                    Logging.info $"Attempting to reconnect, attempt: %d{attempt}."

                    cancellationTokenSource |> Option.iter (fun cts -> cts.Cancel())
                    client.Dispose()

                    let client = createConnection connection

                    try
                        do! client |> connect
                    with _ ->
                        let delay = backoff attempt
                        do! Async.Sleep delay
                        mb.Post (Reconnect (attempt+1))

                    return client
                }

            let sendWhisper userId message accessToken =
                async {
                    if whisperRateLimiter.CanSend() then
                        Logging.info $"Sending whisper: %s{userId} %s{message}"
                        do! twitchService.SendWhisper config.UserId userId message accessToken |> Async.Ignore
                }

            let sendMessage (message: IRC.Request) (client: IConnection) =
                async {
                    let raw = message |> Request.toString
                    Logging.info $"Sending: %s{raw}"
                    do! client.SendAsync(raw, cancellationToken)
                }

            let rec loop (client: IConnection) messageTimestamps =
                async {
                    let! request = mb.Receive()

                    match request with
                    | Start -> do! client |> connect
                    | SendIrc r ->
                        if client.Connected then
                            match r with
                            | PrivMsg (channel, _) when chatRateLimiter.CanSend() ->
                                match messageTimestamps |> Map.tryFind channel with
                                | None ->
                                    do! client |> sendMessage r
                                    return! loop client (messageTimestamps |> Map.add channel (epochTime()))
                                | Some timestamp when epochTime() - timestamp > GlobalSlow ->
                                    do! client |> sendMessage r
                                    return! loop client (messageTimestamps |> Map.add channel (epochTime()))
                                | _ ->
                                    do! Async.Sleep 100
                                    mb.Post request
                            | PrivMsg _ ->
                                do! Async.Sleep 100
                                mb.Post request
                            | _ ->
                                do! client |> sendMessage r
                        else
                            do! Async.Sleep 100
                            mb.Post request
                    | SendWhisper (userId, message, accessToken) -> do! sendWhisper userId message accessToken
                    | Reconnect attempt ->
                        let! client = reconnect attempt client
                        return! loop client messageTimestamps

                    return! loop client messageTimestamps
                }

            let client = createConnection connection
            loop client Map.empty
        , cancellationToken)

        let start () = agent.Post Start
        let reconnect () = agent.Post (Reconnect 0)
        let sendIrc request = agent.Post (SendIrc request)
        let sendWhisper (userId, message, accessToken) = agent.Post (SendWhisper (userId, message, accessToken))

        {
            Start = start
            Reconnect = reconnect
            Send = fun r -> sendIrc r
            SendWhisper = fun userId message accessToken -> sendWhisper (userId,message,accessToken)
            MessageReceived = messageReceived.Publish
        }

module TwitchChatClientConfig =

    let (|IRC|_|) : string -> string option =
        function
        | s when s.StartsWith("irc://") -> Some s
        | _ -> None

    let (|WebSocket|_|) : string -> string option =
        function
        | s when s.StartsWith("wss://") -> Some s
        | _ -> None

    let connectionConfig : string -> ConnectionType =
        function
        | IRC uri ->
            let uri = new Uri(uri)
            ConnectionType.IRC(uri.Host, uri.Port)
        | WebSocket uri ->
            let uri = new Uri(uri)
            ConnectionType.Websocket(uri.Host, uri.Port)
        | _ -> failwith "Unsupported connection protocol set"