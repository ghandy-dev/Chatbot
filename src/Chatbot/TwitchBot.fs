module Chatbot.Bot

open System

open FsToolkit.ErrorHandling

open Chatbot.Agents
open Chatbot.CompositionRoot
open Chatbot.Configuration
open Chatbot.Core
open Chatbot.Core.Domain.Messages
open Chatbot.Core.IRC
open Chatbot.Core.Services
open Chatbot.Core.Services.Twitch
open Chatbot.Twitch

let getChannels () =
    async {
        let! channels = Chatbot.Database.Channels.getAll db
        let channelIds = channels |> Seq.map _.ChannelId

        match! twitchService.Users.GetUsersById channelIds with
        | Error _ ->
            Logging.warning "Twitch API error, falling back on database channel names"
            return channels |> Seq.map (fun c -> c.ChannelId, c.ChannelName)
        | Ok channels ->
            return channels |> Seq.map (fun u -> u.Id, u.Login)
    }

let getAccessToken (twitchService: TwitchService) =
        twitchService.Authentication.GetAccessToken ()
        |> AsyncResult.map _.AccessToken
        |> AsyncResult.mapError (fun statusCode ->
            statusCode
            |> Http.HttpStatusCode.fromInt
            |> fun (err: Http.Types.HttpStatusCode) -> $"Failed to get access token: {err}")

let getAccessTokenUser (twitchService: TwitchService) token =
    twitchService.Users.GetAccessTokenUser token
    |> AsyncResult.mapError (fun statusCode ->
        statusCode
        |> Http.HttpStatusCode.fromInt
        |> fun err -> $"Failed to get access token user: {err}")
        |> AsyncResult.map (fun user -> (user, token))

let authenticate (twitchClient: TwitchClient) =
    async {
        match!
            getAccessToken twitchService
            |> AsyncResult.bind (getAccessTokenUser twitchService)
        with
        | Ok (user, token) ->
            twitchClient.Send (Request.capReq configuration.TwitchChatConfig.Capabilities)
            twitchClient.Send (Request.pass token)
            twitchClient.Send (Request.nick user.Login)
        | Error err ->
            Logging.error err
    }

let joinChannels (twitchClient: TwitchClient) =
    async {
        let! channels = getChannels () |> Async.map (Seq.map snd)
        let message = Request.joinMultiple channels
        twitchClient.Send message
    }

let run (cancellationToken: Threading.CancellationToken) =
    async {
        let uri = new Uri(configuration.ConnectionStrings.IrcServer)
        let twitchClient = new TwitchClient(uri.Host, uri.Port)

        let reminderAgent = Reminder.create db pastebinService twitchClient cancellationToken
        let triviaAgent = Trivia.create twitchClient cancellationToken
        let botAgent = Bot.create botConfig emoteService db configuration.UserId twitchClient triviaAgent cancellationToken

        twitchClient.MessageReceived.Subscribe(fun message ->
            match message |> tryMapMessage with
            | Some m ->
                reminderAgent.Post (Reminder.TwitchEvent m)
                triviaAgent.Post (Trivia.TwitchEvent m)
                botAgent.Post (Bot.TwitchEvent m)
            | None -> ()
        ) |> ignore

        botAgent.Start()
        reminderAgent.Start()
        triviaAgent.Start()

        twitchClient.Connect()
        twitchClient.Start()

        do! authenticate twitchClient
        do! joinChannels twitchClient
    }
