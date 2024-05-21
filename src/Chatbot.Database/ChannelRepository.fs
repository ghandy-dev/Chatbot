namespace Chatbot.Database

module ChannelRepository =

    open DB
    open Dapper.FSharp.SQLite
    open Types

    let private mapChannelEntity (entity: Entities.Channel) : Channel = {
        ChannelId = entity.channel_id.ToString()
        ChannelName = entity.channel_name
    }

    let private mapChannel (record: Channel) : Entities.Channel = {
        channel_id = record.ChannelId |> int
        channel_name = record.ChannelName
    }

    let getAll () =
        async {
            let! channel =
                select {
                    for row in channels do
                        selectAll
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapChannelEntity
        }

    let getAllActive () =
        async {
            let! channel =
                select {
                    for row in channels do
                        selectAll
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapChannelEntity
        }

    let getById (channelId: int) =
        async {
            let! channel =
                select {
                    for row in channels do
                        where (row.channel_id = channelId)
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapChannelEntity |> Seq.tryExactlyOne
        }

    let add (channel: Channel) =
        async {
            let newChannel = mapChannel channel

            try
                let! rowsAffected =
                    insert {
                        into channels
                        value newChannel
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return Success rowsAffected
            with ex ->
                return Failure ex
        }

    let delete (channelId: int) =
        async {
            try
                let! rowsAffected =
                    delete {
                        for row in channels do
                            where (row.channel_id = channelId)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return Success rowsAffected
            with ex ->
                return Failure ex
        }
