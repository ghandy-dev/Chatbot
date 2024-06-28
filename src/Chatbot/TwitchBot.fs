module Chatbot.Bot

open Chatbot
open Chatbot.Configuration
open Chatbot.Commands
open Chatbot.IRC
open Chatbot.MessageHandlers
open Chatbot.MessageLimiter
open Chatbot.Shared
open Chatbot.Types

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
}

let init () =
    async {
        let! channels = Database.ChannelRepository.getAll () |+-> List.ofSeq |+-> List.map (fun c -> c.ChannelName)

        return {
            Channels = channels
            RoomStates = Map.empty
        }
    }

let createIrcClient () =
    new IrcClient(fst ircConnection, snd ircConnection |> int)

let authenticate (client: IrcClient) =
    async {
        logger.LogTrace "Authenticating..."
        if (botConfig.Capabilities.Length > 0) then
            logger.LogTrace "Requesting Capabilities..."
            let capabilities = String.concat " " botConfig.Capabilities
            do! client.WriteAsync($"CAP REQ :{capabilities}")

        do! client.WriteAsync($"PASS oauth:{accessToken}")
        do! client.WriteAsync($"NICK {botConfig.Botname}")
        do! client.FlushAsync()
    }

let createBot (client: IrcClient) cancellationToken =

    logger.LogTrace "Creating bot..."

    let chatRateLimiter = RateLimiter(Rates.MessageLimit_Chat, Rates.Interval_Chat)
    let whisperRateLimiter = RateLimiter(Rates.MessageLimit_Whispers, Rates.Interval_Whispers)

    let rec ircReader (client: IrcClient) (mb: MailboxProcessor<_>) (state: State) =
        async {
            match client.Connected with
            | true ->
                match! client.ReadAsync cancellationToken with
                | Some messages ->
                    logger.LogInfo(messages.TrimEnd([| '\r' ; '\n' |]))
                    do! handleMessages messages mb
                    do! ircReader client mb state
                | None -> () // if this happens then the client isn't connected
            | false -> logger.LogInfo("IRC client disconnected.")
        }

    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {

                let start (client: IrcClient) =
                    logger.LogTrace "Starting bot..."
                    async {
                        logger.LogTrace "Initialising state..."
                        let! state = init ()

                        logger.LogTrace "Joining channels..."
                        do! client.JoinChannels state.Channels

                        logger.LogTrace "Starting reader..."
                        let! result = ircReader client mb state |> Async.StartChild |> Async.Catch

                        match result with
                        | Choice1Of2 _ -> logger.LogTrace "Reader returned."
                        | Choice2Of2 ex -> logger.LogError($"Exception occurred in {nameof (ircReader)}", ex)
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
                            logger.LogInfo($"Sending PONG :{pong}")
                            do! client.PongAsync(pong)
                        | SendPrivateMessage pm ->
                            logger.LogInfo($"Sending private message: #{pm.Channel} {pm.Message}")
                            do! client.SendPrivMessage(pm.Channel, pm.Message)
                        | SendWhisperMessage wm ->
                            if (whisperRateLimiter.CanSend()) then
                                match! HelixApi.Users.getUser (botConfig.Botname) |+-> TTVSharp.tryHead with
                                | None -> do logger.LogError($"Could not look up own User {botConfig}", new Exception())
                                | Some user ->
                                    match! HelixApi.Whispers.sendWhisper user.Id wm.UserId wm.Message accessToken with
                                    | 204 -> logger.LogInfo($"Whisper sent to {wm.Username}: {wm.Message}")
                                    | 400 -> logger.LogInfo("Failed to send whisper. Bad request")
                                    | 401 ->
                                        logger.LogInfo(
                                            "Failed to send whisper. Things to check: Verified phone number, user access token has user:manage:whispers"
                                        )
                                    | 403 -> logger.LogInfo("Failed to send whisper. Suspended or account can't send whispers")
                                    | 404 -> logger.LogInfo("Failed to send whisper. Recipient user id was not found")
                                    | 429 -> logger.LogInfo("Failed to send whisper. Exceeded rate limit")
                                    | statusCode -> logger.LogInfo($"Unexpected response from Helix Whisper API: {statusCode}")
                        | SendRawIrcMessage msg ->
                            logger.LogInfo($"Sending raw message: {msg}")
                            do! send msg client
                        | BotCommand command ->
                            match command with
                            | JoinChannel channel -> do! client.JoinChannel channel
                            | LeaveChannel channel -> do! client.PartChannel channel
                        | Reconnect ->
                            logger.LogInfo("Twitch servers requested we reconnect...")
                            (client :> IDisposable).Dispose()
                            let client = createIrcClient ()
                            do! start client
                            do! loop client

                        do! loop client
                    }

                do! authenticate client
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
