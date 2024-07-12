module Chatbot.Bot

open Chatbot
open Chatbot.Commands
open Chatbot.IRC
open Chatbot.MessageHandlers
open Chatbot.MessageLimiter
open Chatbot.Shared
open Chatbot.Types

open Authorization
open TTVSharp

open System

type RoomState = {
    LastMessage: DateTime
    Channel: string
    EmoteOnly: bool option
    FollowersOnly: bool option
    R9k: bool option
    RoomId: string
    Slow: int option
    SubsOnly: bool option
}

type State = {
    Channels: string list
    RoomStates: Map<string, RoomState>
    BotUser: string
    BotUserId: string
}

let init () =
    async {
        let! channels = Database.ChannelRepository.getAll () |+> List.ofSeq |+> List.map (fun c -> c.ChannelName)
        let! maybeToken = tokenStore.GetToken(TokenType.Twitch)

        match maybeToken with
        | None -> return failwith "Could not retrieve token using ClientId and ClientSecret"
        | Some token ->
            let! maybeUser =
                Helix.helixApi.Users.GetUsersAsync(token) |> Async.AwaitTask
                |+> Helpers.Helix.tryHead

            match maybeUser with
            | None -> return failwith "No user associated with token?"
            | Some user ->
                return {
                    Channels = channels
                    RoomStates = Map.empty
                    BotUser = user.DisplayName
                    BotUserId = user.Id
                }
    }

let createIrcClient () =
    new IrcClient(fst ircConnection, snd ircConnection |> int)

let authenticate (client: IrcClient) user =
    async {
        Logging.trace "Authenticating..."

        match! Authorization.tokenStore.GetToken Authorization.TokenType.Twitch with
        | None -> failwithf "Couldn't retrieve access token for Twitch API"
        | Some token ->

            if (botConfig.Capabilities.Length > 0) then
                Logging.trace "Requesting Capabilities..."
                let capabilities = String.concat " " botConfig.Capabilities
                do! client.WriteAsync($"CAP REQ :{capabilities}")

            do! client.WriteAsync($"PASS oauth:{token}")
            do! client.WriteAsync($"NICK {user}")
            do! client.FlushAsync()
    }

let createBot (client: IrcClient) cancellationToken =

    Logging.trace "Creating bot..."

    let chatRateLimiter = RateLimiter(Rates.MessageLimit_Chat, Rates.Interval_Chat)

    let whisperRateLimiter =
        RateLimiter(Rates.MessageLimit_Whispers, Rates.Interval_Whispers)

    let rec ircReader (client: IrcClient) (mb: MailboxProcessor<_>) (state: State) =
        async {
            match client.Connected with
            | true ->
                match! client.ReadAsync cancellationToken with
                | Some message ->
                    Logging.info (message.Trim([| '\r' ; '\n' |]))
                    do! handleMessage message mb
                    do! ircReader client mb state
                | None -> () // if this happens then the client isn't connected
            | false -> Logging.warning "IRC client disconnected."
        }

    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {

                let! state = init ()

                let start (client: IrcClient) =
                    Logging.trace "Starting bot..."

                    async {
                        Logging.trace "Joining channels..."
                        do! client.JoinChannels state.Channels

                        Logging.trace "Starting reader..."
                        let! result = ircReader client mb state |> Async.StartChild |> Async.Catch

                        match result with
                        | Choice1Of2 _ -> Logging.trace "Reader started."
                        | Choice2Of2 ex -> Logging.error $"Exception occurred in {nameof (ircReader)}" ex
                    }

                let send (message: string) (client: IrcClient) =
                    async {
                        if chatRateLimiter.CanSend() then
                            do! client.SendAsync(message)
                    }

                let rec loop (client: IrcClient) =
                    async {
                        match! mb.Receive() with
                        | SendPongMessage pong ->
                            Logging.info $"Sending PONG :{pong}"
                            do! client.PongAsync(pong)
                        | SendPrivateMessage pm ->
                            Logging.info $"Sending private message: #{pm.Channel} {pm.Message}"
                            do! client.SendPrivMessage(pm.Channel, pm.Message)
                        | SendWhisperMessage wm ->
                            if (whisperRateLimiter.CanSend()) then
                                match! Authorization.tokenStore.GetToken Authorization.TokenType.Twitch with
                                | None -> Logging.info "Failed to send whisper. Couldn't retrieve access token for Twitch API."
                                | Some token ->
                                    match! Helix.Whispers.sendWhisper state.BotUserId wm.UserId wm.Message token with
                                    | 204 -> Logging.info $"Whisper sent to {wm.Username}: {wm.Message}"
                                    | 400 -> Logging.info "Failed to send whisper. Bad request"
                                    | 401 -> Logging.info "Failed to send whisper. Things to check: Token refreshed, verified phone number, token has user:manage:whispers scope"
                                    | 403 -> Logging.info "Failed to send whisper. Suspended or account can't send whispers"
                                    | 404 -> Logging.info "Failed to send whisper. Recipient user id was not found"
                                    | 429 -> Logging.info "Failed to send whisper. Exceeded rate limit"
                                    | statusCode -> Logging.info $"Unexpected response from Helix Whisper API: {statusCode}"
                        | SendRawIrcMessage msg ->
                            Logging.info $"Sending raw message: {msg}"
                            do! send msg client
                        | BotCommand command ->
                            match command with
                            | JoinChannel channel -> do! client.JoinChannel channel
                            | LeaveChannel channel -> do! client.PartChannel channel
                        | Reconnect ->
                            Logging.info "Twitch servers requested we reconnect..."
                            (client :> IDisposable).Dispose()
                            let client = createIrcClient ()
                            do! authenticate client state.BotUser
                            do! start client
                            do! loop client

                        do! loop client
                    }

                do! authenticate client state.BotUser
                do! start client
                do! loop client

            }
        ),
        cancellationToken
    )

let run (cancellationToken: Threading.CancellationToken) =
    let client = createIrcClient ()
    let mb = createBot client cancellationToken

    mb.StartImmediate()
