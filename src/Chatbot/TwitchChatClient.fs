module Chatbot.Twitch

open System
open System.Collections.Generic
open System.Threading

open Connection
open Chatbot.Core
open Chatbot.Core.IRC
open Chatbot.Core.IRC.Messages
open RateLimiter

type State = {
    Connection: Connection
    Channels: Set<string>
}

type TwitchClientMessage =
    | Start
    | Connect
    | Reconnect of attempt: int
    | ClientDisconnected
    | MessageReceived of string
    | Send of Request

let parseMessage (message: string) =
    message.Split("\r\n")
    |> Array.map (IRC.Parsing.parse >> IRC.Messages.Mapping.parse)
    |> Array.choose id

module Agent =

    let create
        (host: string)
        (port: int)
        (onMessage: IrcMessage -> unit)
        cancellationToken =

        let mutable cancellationTokenSource: CancellationTokenSource option = None
        let chatRateLimiter = new RateLimiter(RateLimiter.MessageLimit_Chat, RateLimiter.ResetInterval_Chat)
        let messageQueue = new Queue<Request>()

        let initial = {
            Connection = new Connection(host, port)
            Channels = Set.empty
        }

        MailboxProcessor<TwitchClientMessage>.Start((fun mb ->

            let backoff attempt =
                match attempt with
                | 0 | 1 | 2 | 4 as x -> pown x 4
                | _ -> pown 5 4
                |> fun s -> TimeSpan.FromSeconds(int64 s)

            let handlePing (message: PingMessage) = mb.Post (Send (Request.pong message.Message))

            let handleReconnect () = mb.Post (Reconnect 0)

            let handleMessage message =
                match message with
                | PingMessage m -> handlePing m
                | ReconnectMessage -> handleReconnect ()
                | _ -> ()

            let readerLoop (connection: Connection) =
                async {
                    Logging.info "Reader starting..."

                    let rec loop () =
                        async {
                            match! connection.ReadAsync cancellationToken with
                            | Ok data ->
                                mb.Post (MessageReceived data)
                                return! loop ()
                            | Error ex ->
                                Logging.errorEx "Reader loop error" ex
                                mb.Post ClientDisconnected
                                return ()
                            return! loop ()
                        }

                    return! loop ()
                }

            let connect (connection: Connection) =
                async {
                    Logging.info "Connecting..."
                    let! result = connection.ConnectAsync cancellationToken

                    match result with
                    | Ok _ -> ()
                    | _ -> mb.Post (Reconnect 0)
                }

            let start (connection: Connection) =
                async {
                    Logging.info "Starting..."
                    let cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    cancellationTokenSource <- Some cts

                    Async.Start(readerLoop connection, cts.Token)
                }

            let reconnect attempt state =
                async {
                    Logging.info "Reconnecting..."
                    cancellationTokenSource |> Option.iter (fun cts -> cts.Cancel())

                    (state.Connection :> IDisposable).Dispose()

                    let connection = new Connection(host, port)

                    let! result = connection.ConnectAsync cancellationToken

                    match result with
                    | Ok _ -> mb.Post Start
                    | _ ->
                        let sleepTime = backoff attempt
                        do! Async.Sleep(sleepTime)
                        mb.Post (Reconnect (attempt+1))

                    return { state with Connection = connection }
                }

            let send (connection: Connection) (request: Request) (state: State) =
                async {
                    let mutable channels' = state.Channels

                    messageQueue.Enqueue(request)

                    while messageQueue.Count > 0 do
                        let requestOpt =
                            match messageQueue.Dequeue() with
                            | PrivMsg (channel, _) ->
                                if chatRateLimiter.CanSend channel then
                                    Some request
                                else
                                    messageQueue.Enqueue(request)
                                    None
                            | _ ->
                                Some request

                        match requestOpt with
                        | Some r ->
                            let message = r |> Request.toString
                            Logging.info $"Sending: {message}"

                            match! connection.SendAsync(message, cancellationToken) with
                            | Ok _ -> ()
                            | Error err -> Logging.errorEx "Error sending message" err
                        | None -> ()

                        match requestOpt with
                        | Some (Join channel) -> channels' <- state.Channels |> Set.add channel
                        | Some (JoinM channels) ->
                            channels' <-
                                channels
                                |> Seq.fold (fun acc channel ->
                                    acc |> Set.add channel
                                ) state.Channels
                        | Some _
                        | None -> ()

                    return { state with Channels = channels' }
                }

            let messageReceived message state =
                async {
                    Logging.info $"Receieved: {message}"
                    let messages = message |> parseMessage

                    messages
                    |> Seq.iter (fun message ->
                        handleMessage message
                        onMessage message
                    )

                    return state
                }

            let rec loop state =
                async {
                    let! message = mb.Receive()

                    match message with
                    | Connect -> do! connect state.Connection
                    | Start -> do! start state.Connection
                    | ClientDisconnected -> mb.Post (Reconnect 0)
                    | Reconnect attempt ->
                        let! state' = reconnect attempt state
                        return! loop state'
                    | MessageReceived message ->
                        let! state' = messageReceived message state
                        return! loop state'
                    | Send request ->
                        let! state' = send state.Connection request state
                        return! loop state'

                    return! loop state
                }

            loop initial
            ), cancellationToken
        )

type TwitchClient (host, port) =

    let cancellationTokenSource = new CancellationTokenSource()
    let messageReceived = new Event<IrcMessage>()

    let agent = Agent.create host port (fun message -> messageReceived.Trigger message) cancellationTokenSource.Token

    [<CLIEvent>]
    member _.MessageReceived = messageReceived.Publish

    member _.Start () = agent.Post Start

    member _.Connect () = agent.Post Connect

    member _.Send message = agent.Post (Send message)

    member _.SendWhisper (fromUserId, toUserId, message) = ()
