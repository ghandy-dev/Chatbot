module Chatbot.Twitch

open System
open System.Collections.Generic
open System.Threading

open Microsoft.Extensions.Logging

open FSharpPlus
open FsToolkit.ErrorHandling

open Chatbot.Connection
open Chatbot.Core
open Chatbot.Core.IRC
open Chatbot.Core.IRC.Messages
open RateLimiter
open Chatbot.Core.Services.Twitch

type ConnectionState =
    | Disconnected
    | Connected
    | Authenticating
    | Ready

type State = {
    Connection: Connection
    Channels: Set<string>
    ConnectionState: ConnectionState
    Username: string option
    MessageQueue: Queue<Request>
}

type TwitchClientMessage =
    | Connect
    | Authenticate
    | Authenticated
    | JoinChannels
    | Reconnect of attempt: int
    | ClientDisconnected
    | MessageReceived of string
    | Send of Request
    | SendWhisper of fromUserId: string * toUserId: string * message: string

let getAccessToken (twitchService: TwitchService) =
    twitchService.Authentication.GetAccessToken ()
    |> AsyncResult.map _.AccessToken
    |> AsyncResult.mapError (
        Http.HttpStatusCode.fromInt
        >> fun err -> $"Failed to get access token: {err}"
    )

let getAccessTokenUser (twitchService: TwitchService) token =
    twitchService.Users.GetAccessTokenUser token
    |> AsyncResult.mapError (
        Http.HttpStatusCode.fromInt
        >> fun err -> $"Failed to get access token user: {err}"
    )
    |> AsyncResult.map _.DisplayName

let sendWhisper (twitchService: TwitchService) fromUserId toUserId message accessToken =
    twitchService.Whispers.SendWhisper fromUserId toUserId message accessToken
    |> AsyncResult.mapError (Http.HttpStatusCode.fromInt >> fun err -> $"Failed to send whisper: {err}")

let parseMessage (message: string) =
    message.Split("\r\n")
    |> Array.map (IRC.Parsing.parse >> IRC.Messages.Mapping.parse)
    |> Array.choose id

module Agent =

    let create
        (host: string)
        (port: int)
        (configuration: Configuration.TwitchChatConfig)
        (logger: ILogger)
        (channels: Set<string>)
        twitchService
        (onMessage: IrcMessage -> unit)
        cancellationToken =

        let mutable cancellationTokenSource: CancellationTokenSource option = None

        let chatRateLimiter = new RateLimiter(RateLimiter.MessageLimit_Chat, RateLimiter.ResetInterval_Chat)

        let whisperRateLimiter = new RateLimiter(RateLimiter.MessageLimit_Whispers, RateLimiter.ResetInterval_Whispers)

        let initial = {
            Connection = new Connection(host, port)
            Channels = channels
            ConnectionState = Disconnected
            Username = None
            MessageQueue = new Queue<Request>()
        }

        MailboxProcessor<TwitchClientMessage>.Start((fun mb ->

            let backoff attempt =
                match attempt with
                | 0 | 1 | 2 | 4 as x -> pown x 4
                | _ -> pown 5 4
                |> fun s -> TimeSpan.FromSeconds(int64 s)

            let sendRequest (connection: Connection) request =
                async {
                    let message = request |> Request.toString
                    logger.LogInformation("Sending: {message}", message)

                    match! connection.SendAsync(message, cancellationToken) with
                    | Ok _ -> ()
                    | Error ex -> logger.LogWarning(ex, "Error occurred sending message")
                }

            let handleMessage connection message =
                async {
                    match message with
                    | ConnectedMessage ->
                        mb.Post Authenticated
                        mb.Post JoinChannels
                    | PingMessage m -> do! sendRequest connection (Request.pong m.Message)
                    | ReconnectMessage -> mb.Post (Reconnect 1)
                    | _ -> ()
                }

            let readerLoop (connection: Connection) =
                async {
                    logger.LogInformation "Reader starting..."

                    let rec loop () =
                        async {
                            match! connection.ReadAsync cancellationToken with
                            | Ok null ->
                                logger.LogWarning("null message received")
                                mb.Post ClientDisconnected
                                return ()
                            | Ok data ->
                                mb.Post (MessageReceived data)
                                return! loop ()
                            | Error ex ->
                                logger.LogWarning(ex, "Reader loop error")
                                mb.Post ClientDisconnected
                                return ()
                        }

                    return! loop ()
                }

            let authenticate state =
                async {
                    let! result =
                        asyncResult {
                            let! token = getAccessToken twitchService

                            let! username =
                                state.Username
                                |> Option.toResultWith "Username has not been set"
                                |> Async.singleton
                                |> AsyncResult.orElse (getAccessTokenUser twitchService token)

                            return token, username
                    }

                    match result with
                    | Ok (token, username) ->
                        do! sendRequest state.Connection (Request.capReq configuration.Capabilities)
                        do! sendRequest state.Connection (Request.pass token)
                        do! sendRequest state.Connection (Request.nick username)

                        return { state with Username = Some username }
                    | Error err ->
                        logger.LogError("Error requesting access token: {err}", err)
                        return state
                }

            let joinChannels state =
                async {
                    if not (state.Channels |> Set.isEmpty) then
                        do! sendRequest state.Connection (Request.joinMultiple state.Channels)
                    else
                        logger.LogInformation("No channels set to join")
                }

            let start (connection: Connection) =
                async {
                    logger.LogInformation("Starting...")
                    let cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    cancellationTokenSource <- Some cts

                    Async.Start(readerLoop connection, cts.Token)
                }

            let connect (connection: Connection) state =
                async {
                    logger.LogInformation("Connecting...")
                    let! result = connection.ConnectAsync cancellationToken

                    match result with
                    | Ok _ ->
                        do! start connection
                        mb.Post Authenticate
                        return { state with ConnectionState = Connected }
                    | Error ex ->
                        logger.LogWarning(ex, "Error trying to connect to host")
                        mb.Post (Reconnect 1)
                        return { state with ConnectionState = Disconnected }
                }

            let reconnect attempt state =
                async {
                    logger.LogInformation("Reconnecting...")

                    cancellationTokenSource |> Option.iter (fun cts -> cts.Cancel() ; cts.Dispose() )

                    (state.Connection :> IDisposable).Dispose()

                    let connection = new Connection(host, port)

                    let sleepTime = backoff attempt
                    do! Async.Sleep(sleepTime)

                    let! result = connection.ConnectAsync cancellationToken

                    match result with
                    | Ok _ ->
                        do! start connection
                        mb.Post Authenticate
                        return { state with ConnectionState = Connected ; Connection = connection }
                    | Error ex ->
                        logger.LogWarning(ex, "Error trying to connect to host")
                        mb.Post (Reconnect (attempt+1))
                        return { state with ConnectionState = Disconnected ; Connection = connection }
                }

            let rec flush (connection: Connection) (state: State) =
                async {
                    if state.MessageQueue.Count > 0 then
                        let requestOpt =
                            let request = state.MessageQueue.Dequeue()

                            match request with
                            | PrivMsg (channel, _) ->
                                if chatRateLimiter.CanSend channel then
                                    Some request
                                else
                                    state.MessageQueue.Enqueue(request)
                                    None
                            | _ ->
                                Some request

                        match requestOpt with
                        | Some r -> do! sendRequest connection r
                        | None -> ()

                        let channels =
                            match requestOpt with
                            | Some (Join channel) -> state.Channels |> Set.add channel
                            | Some (JoinM channels) ->
                                channels
                                |> Seq.fold (fun acc channel ->
                                    acc |> Set.add channel
                                ) state.Channels
                            | Some _
                            | None -> state.Channels

                        return! flush connection { state with Channels = channels }
                    else
                        return state
                }

            let sendWhisper (fromUserId, toUserId, message) (state: State) =
                async {
                    if whisperRateLimiter.CanSend "whisper" then
                        logger.LogInformation($"Sending whisper ({fromUserId} -> {toUserId}) : {message}")
                        match!
                            getAccessToken twitchService
                            |> AsyncResult.bind (sendWhisper twitchService fromUserId toUserId message)
                        with
                        | Error err ->
                            logger.LogWarning("Failed to send whisper: {err}", err)
                            return state
                        | Ok _ ->
                            return state
                    else
                        return state
                }

            let messageReceived connection message =
                async {
                    if not (message |> String.isEmpty) then
                        logger.LogInformation("Receieved: {message}", message)
                        let messages = message |> parseMessage

                        for message in messages do
                            do! handleMessage connection message
                            do onMessage message
                }

            let enqueue request state =
                state.MessageQueue.Enqueue(request)
                state

            let rec loop state =
                async {
                    let! message = mb.Receive()

                    match message with
                    | Connect ->
                        let! state' = connect state.Connection state
                        return! loop state'
                    | Authenticate ->
                        let! state' = authenticate state
                        return! loop state'
                    | Authenticated ->
                        let state' = { state with ConnectionState = ConnectionState.Ready }
                        let! state'' = flush state.Connection state'
                        return! loop state''
                    | JoinChannels ->
                        do! joinChannels state
                        return! loop state
                    | ClientDisconnected ->
                        mb.Post (Reconnect 1)
                        return! loop { state with ConnectionState = Disconnected }
                    | Reconnect attempt ->
                        let! state' = reconnect attempt state
                        return! loop state'
                    | MessageReceived message ->
                        do! messageReceived state.Connection message
                        return! loop state
                    | Send request ->
                        let state' = enqueue request state

                        if state.ConnectionState = ConnectionState.Ready then
                            let! state'' = flush state.Connection state
                            return! loop state''
                        else
                            return! loop state'
                    | SendWhisper (fromUserId, toUserId, message) ->
                        let! state' = sendWhisper (fromUserId, toUserId, message) state
                        return! loop state'
                }

            loop initial
            ), cancellationToken
        )

type TwitchClient (host, port, configuration, logger, twitchService, channels) =

    let cancellationTokenSource = new CancellationTokenSource()
    let messageReceived = new Event<IrcMessage>()

    let agent = Agent.create host port configuration logger channels twitchService (fun message -> messageReceived.Trigger message) cancellationTokenSource.Token

    [<CLIEvent>]
    member _.MessageReceived = messageReceived.Publish

    member _.Start () = agent.Post Connect

    member _.Send message = agent.Post (Send message)

    member _.SendWhisper (fromUserId, toUserId, message) = agent.Post (SendWhisper (fromUserId, toUserId, message))
