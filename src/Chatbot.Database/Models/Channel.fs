namespace Database.Models

type Channel = {
    ChannelId: string
    ChannelName: string
}

type NewChannel = {
    ChannelId: string
    ChannelName: string
} with

    static member create channelId channelName = {
        ChannelId = channelId
        ChannelName = channelName
    }