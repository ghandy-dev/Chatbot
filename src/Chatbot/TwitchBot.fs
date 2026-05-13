module Chatbot.Bot

open System

open Microsoft.Extensions.Logging

open FSharpPlus

open Chatbot.Agents
open Chatbot.CompositionRoot
open Chatbot.Configuration
open Chatbot.Core.Domain.Messages
open Chatbot.Core.Domain.Types
open Chatbot.Core.Services.Twitch
open Chatbot.Twitch

let getChannels (channels: IChannelRepository) (twitchService: TwitchService) =
    async {
        let! channels = channels.GetAll ()
        let channelIds = channels |> Seq.map (_.ChannelId >> string)

        match! twitchService.Users.GetUsersById channelIds with
        | Error _ ->
            logger.LogWarning("Twitch API error, falling back on database channel names")
            return channels |> Seq.map (fun c -> string c.ChannelId, c.ChannelName)
        | Ok channels ->
            return channels |> Seq.map (fun u -> u.Id, u.Login)
    }

let run (cancellationToken: Threading.CancellationToken) =
    async {
        let uri = new Uri(configs.ConnectionStrings.IrcServer)
        let! channels = getChannels channels twitchService |> Async.map (Seq.map snd >> Set.ofSeq)
        let twitchClient = new TwitchClient(uri.Host, uri.Port, configs.TwitchChatConfig, loggerFactory.CreateLogger<TwitchClient>(), twitchService, channels)

        let reminderAgent = Reminder.create env reminders pastebinService twitchClient cancellationToken
        let triviaAgent = Trivia.create env twitchClient cancellationToken
        let botAgent = Bot.create env botConfig users aliases emoteService configs.UserId twitchClient triviaAgent cancellationToken

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
