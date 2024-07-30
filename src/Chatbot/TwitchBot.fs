module Chatbot.Bot

open Chatbot
open Chatbot.Commands
open Chatbot.MessageHandlers
open Chatbot.Types

open Authorization
open TTVSharp

open System

let botConfig = Configuration.Bot.config

let user =
    async {
        match! tokenStore.GetToken(TokenType.Twitch) |> Option.bindAsync Helix.Users.getAccessTokenUser with
        | Some user -> return user
        | None -> return failwith "Expect access token (or user?)"
    }
    |> Async.RunSynchronously

let connectionConfig =
    let uri = new Uri(Configuration.ConnectionStrings.config.Twitch)
    match uri.Scheme with
    | "irc" -> ConnectionType.IRC(uri.Host, uri.Port)
    | "wss" -> ConnectionType.Websocket(uri.Host, uri.Port)
    | _ -> failwith "Bad connection string - expected format: scheme://host:port"

let twitchChatConfig: TwitchChatClientConfig = {
    Username = user.DisplayName
    Capabilities = botConfig.Capabilities
}

let getChannels () =
    async { return! Database.ChannelRepository.getAll () |-> List.ofSeq |-> List.map (fun c -> c.ChannelName) }

let createBot (twitchChatClient: TwitchChatClient) cancellationToken =
    new MailboxProcessor<ClientRequest>(
        (fun mb ->
            async {

                let start () =
                    async {
                        Logging.trace "Starting bot..."
                        do! twitchChatClient.StartAsync(cancellationToken)
                    }

                let joinChannels () =
                    async {
                        let! channels = getChannels ()
                        do! twitchChatClient.JoinChannelsAsync(channels)
                    }

                let rec loop () =
                    async {
                        match! mb.Receive() with
                        | SendPongMessage pong ->
                            Logging.info $"PONG :{pong}"
                            do! twitchChatClient.Client.PongAsync(pong)
                        | SendPrivateMessage pm -> do! twitchChatClient.SendAsync(pm.Channel, pm.Message)
                        | SendWhisperMessage wm ->
                            match! tokenStore.GetToken TokenType.Twitch with
                            | None -> Logging.info "Failed to send whisper. Couldn't retrieve access token for Twitch API."
                            | Some token ->
                                match! Helix.Whispers.sendWhisper user.Id wm.UserId wm.Message token with
                                | 204 -> Logging.info $"Whisper sent to {wm.Username}: {wm.Message}"
                                | statusCode -> Logging.info $"Failed to send whisper, response from Helix Whisper API: {statusCode}"
                        | SendRawIrcMessage msg -> do! twitchChatClient.SendRawAsync(msg)
                        | BotCommand command ->
                            match command with
                            | JoinChannel channel -> do! twitchChatClient.JoinChannelAsync channel
                            | LeaveChannel channel -> do! twitchChatClient.PartChannelAsync channel
                        | Reconnect ->
                            Logging.info "Twitch servers requested we reconnect..."
                            do! twitchChatClient.ReconnectAsync(cancellationToken)
                            do! joinChannels ()
                            do! loop ()

                        do! loop ()
                    }

                do! start ()
                do! joinChannels ()
                do! loop ()

            }
        ),
        cancellationToken
    )

let run (cancellationToken: Threading.CancellationToken) =
    let twitchChatClient = new TwitchChatClient(connectionConfig, twitchChatConfig)
    let mb = createBot twitchChatClient cancellationToken

    twitchChatClient.MessageReceived.Subscribe (fun message ->
        message |> fun m -> m.Split([| '\r' ; '\n' |], StringSplitOptions.RemoveEmptyEntries) |> Array.iter Logging.trace
        handleMessage message mb |> Async.Start) |> ignore

    mb.StartImmediate()
