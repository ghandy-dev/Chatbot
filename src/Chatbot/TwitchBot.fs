module Bot

open System

open FSharpPlus

open Agents
open Authorization
open Configuration
open Clients
open Database
open IRC
open Types

let services = Services.services

let getAccessToken () =
    async {
        match! tokenStore.GetToken TokenType.Twitch with
        | Ok token -> return token
        | _ -> return failwith "Failed to get access token"
    }

let getAccessTokenUser token =
    async {
        match! services.TwitchService.GetAccessTokenUser token with
        | Ok (Some user) -> return user
        | _ -> return failwith "Failed to look up user associated with access token"
    }

let getChannels () =
    async {
        let! channels = ChannelRepository.getAll ()

        match!
            channels |> Seq.map _.ChannelId |> async.Return
            >>= Twitch.Helix.Users.getUsersById
        with
        | Error _ ->
            Logging.warning "Twitch API error, falling back on database channel names"
            return channels |> Seq.map (fun c -> c.ChannelId, c.ChannelName)
        | Ok channels ->
            return channels |> Seq.map (fun u -> u.Id, u.Login)
    }

let run (cancellationToken: Threading.CancellationToken) =
    async {
        let! accessToken = getAccessToken ()
        let! user = getAccessTokenUser accessToken
        let connectionConfig = TwitchChatClientConfig.connectionConfig appConfig.ConnectionStrings.IrcServer
        let! channels = getChannels ()

        let twitchChatConfig: Clients.TwitchChatClientConfig = {
            UserId = user.Id
            Username = user.DisplayName
            Capabilities = appConfig.Bot.Capabilities
            Channels = channels |> Seq.map snd
        }

        let twitchChatClient =
            Twitch.createClient
                connectionConfig
                twitchChatConfig
                cancellationToken

        let reminderAgent = ReminderAgent.create twitchChatClient cancellationToken
        let triviaAgent = TriviaAgent.create twitchChatClient cancellationToken
        let chatAgent = ChatAgent.create twitchChatClient user triviaAgent cancellationToken

        twitchChatClient.MessageReceived
        |> Event.choose (function | PrivateMessage msg -> Some msg | _ -> None)
        |> Event.add (fun msg ->
            triviaAgent.Post (TriviaRequest.UserMessaged (msg.Channel, int msg.UserId, msg.Username, msg.Message))
            reminderAgent.Post (ReminderMessage.UserMessaged (msg.Channel, int msg.UserId, msg.Username))
        )

        twitchChatClient.MessageReceived
        |> Event.add (fun msg ->
            chatAgent.Post (ClientRequest.HandleIrcMessage msg)
        )

        twitchChatClient.Start ()
        chatAgent.Start()
        reminderAgent.Start()
        triviaAgent.Start()
    }
