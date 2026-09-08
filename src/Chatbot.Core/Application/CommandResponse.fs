namespace Chatbot.Core.Domain.Commands

open Chatbot.Core.Domain

type CommandResponse =
    | Message of string
    | BotAction of BotAction

and BotAction =
    | JoinChannel of channel: string * channelId: string
    | LeaveChannel of channel: string
    | RefreshChannelEmotes of channelId: string
    | RefreshGlobalEmotes of emoteProvider: EmoteProvider
    | StartTrivia of Trivia
    | StopTrivia of channel: string

module CommandResponse =

    let msg message = (Message message)

    let join channel channelId = BotAction (JoinChannel (channel, channelId))
    let leave channel = BotAction (LeaveChannel channel)
    let refreshChannelEmotes channelId = BotAction (RefreshChannelEmotes channelId)
    let refreshGlobalEmotes emoteProvider = BotAction (RefreshGlobalEmotes emoteProvider)
    let startTrivia config = BotAction (StartTrivia config)
    let stopTrivia channel = BotAction (StopTrivia channel)
