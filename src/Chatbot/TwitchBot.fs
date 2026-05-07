module Chatbot.Bot

open System

open FSharpPlus

open Chatbot.Agents
open Chatbot.CompositionRoot
open Chatbot.Configuration
open Chatbot.Core.Domain.Messages
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

let run (cancellationToken: Threading.CancellationToken) =
    async {
        let uri = new Uri(configuration.ConnectionStrings.IrcServer)
        let! channels = getChannels () |> Async.map (Seq.map snd >> Set.ofSeq)
        let twitchClient = new TwitchClient(uri.Host, uri.Port, configuration.TwitchChatConfig, twitchService, channels)

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

        twitchClient.Start()
    }
