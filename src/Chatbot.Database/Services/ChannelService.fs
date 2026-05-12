namespace Chatbot.Database.Services

module ChannelService =

    open Chatbot.Database
    open Chatbot.Database.Types

    let create db =

        {
            new IChannelsService with
                member _.Add channel = Channels.add db channel
                member _.Delete channelId = Channels.delete db channelId
                member _.Get channelId =  Channels.get db channelId
                member _.GetAll() = Channels.getAll db
        }
