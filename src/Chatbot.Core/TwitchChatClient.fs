namespace Chatbot

open Chatbot.IRC

open System

[<RequireQualifiedAccess>]
type ConnectionType = IRC of Host: string * Port: int

type TwitchChatClientConfig = {
    Username: string
    Capabilities: string array
}

type TwitchChatClient(Connection: ConnectionType, Config: TwitchChatClientConfig) =

    let mutable cancellationToken' =
        (new Threading.CancellationTokenSource()).Token

    let messageReceived = new Event<string>()

    let chatRateLimiter = RateLimiter(Rates.MessageLimit_Chat, Rates.Interval_Chat)

    let whisperRateLimiter =
        RateLimiter(Rates.MessageLimit_Whispers, Rates.Interval_Whispers)

    let createClient () =
        match Connection with
        | ConnectionType.IRC(host, port) -> new IrcClient(host, port)

    let mutable client: IrcClient = null

    let send (message: string) =
        async {
            if chatRateLimiter.CanSend() then
                do! client.SendAsync(message)
        }

    let authenticate user =
        async {
            match! Authorization.tokenStore.GetToken Authorization.TokenType.Twitch with
            | None -> failwithf "Couldn't retrieve access token for Twitch API"
            | Some token -> do! client.AuthenticateAsync(user, token, Config.Capabilities)
        }

    let partChannel = client.PartChannel

    let joinChannel = client.JoinChannel

    let joinChannels = client.JoinChannels

    let sendChannelMessage channel message =
        async {
            if chatRateLimiter.CanSend() then
                do! client.SendPrivMessage (channel, message)
        }

    let sendWhisper from ``to`` message accessToken =
        async {
            if (whisperRateLimiter.CanSend()) then
                do! TTVSharp.Helix.Whispers.sendWhisper from ``to`` message accessToken |> Async.Ignore
        }

    let reader (cancellationToken) =
        async {
            let rec reader' () =
                async {
                    match client.Connected with
                    | true ->
                        match! client.ReadAsync cancellationToken with
                        | Some message ->
                            messageReceived.Trigger message
                            do! reader' ()
                        | None -> () // if this happens then the client isn't connected
                    | false -> Logging.warning "IRC client disconnected."
                }

            do! reader' ()
        }

    let start (cancellationToken) =
        async {
            cancellationToken' <- cancellationToken
            client <- createClient ()
            do! authenticate Config.Username

            let! result = reader cancellationToken |> Async.StartChild |> Async.Catch

            match result with
            | Choice1Of2 _ -> Logging.trace "Reader started."
            | Choice2Of2 ex -> Logging.error $"Exception occurred in {nameof (reader)}" ex
        }

    let reconnect () =
        async {
            (client :> IDisposable).Dispose()
            do! start cancellationToken'
        }

    [<CLIEvent>]
    member _.MessageReceived = messageReceived.Publish

    member _.Client with get() = client

    member _.Start (cancellationToken: Threading.CancellationToken) = start (cancellationToken)

    member _.SendRaw message = send message

    member _.Send (channel, message) = sendChannelMessage channel message

    member _.Whisper (from, ``to``, message, accessToken) = sendWhisper from ``to`` message accessToken

    member _.PartChannel channel = partChannel channel

    member _.JoinChannel channel = joinChannel channel

    member _.JoinChannels channels = joinChannels channels

    member _.Reconnect () = reconnect ()
