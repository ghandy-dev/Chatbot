namespace Database

module ChannelRepository =

    open DB
    open Types.Channels

    open Dapper.FSharp.SQLite

    let private mapEntity (entity: Entities.Channel) : Channel = {
        ChannelId = entity.channel_id.ToString()
        ChannelName = entity.channel_name
    }

    let private mapRecord (record: Channel) : Entities.Channel = {
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

            return channel |> Seq.map mapEntity
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

            return channel |> Seq.map mapEntity
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

            return channel |> Seq.map mapEntity |> Seq.tryExactlyOne
        }

    let add (channel: Channel) =
        async {
            let newChannel = mapRecord channel

            try
                let! rowsAffected =
                    insert {
                        into channels
                        value newChannel
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
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

                return DatabaseResult.Success rowsAffected
            with ex ->
                Logging.error ex.Message ex
                return DatabaseResult.Failure
        }
