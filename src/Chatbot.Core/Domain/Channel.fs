namespace Chatbot.Core.Domain

open Types

type Channel =
    {
        ChannelId: ChannelId
        ChannelName: string
    }

type NewChannel =
    {
        ChannelId: ChannelId
        ChannelName: string
    }

    static member create channelId channelName =
        {
            ChannelId = channelId
            ChannelName = channelName
        }
