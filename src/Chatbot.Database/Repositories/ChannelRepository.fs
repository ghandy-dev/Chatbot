namespace Database

module ChannelRepository =

    open DB
    open Database.Models
    open Database.Entities

    open Dapper.FSharp.SQLite

    let mapToModel (channel: Entities.Channel) : Models.Channel = {
        ChannelId = string channel.channel_id
        ChannelName = channel.channel_name
    }

    let getAll () =
        async {
            let! channel =
                select {
                    for row: Channel in channels do
                        selectAll
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapToModel
        }

    let get (channelId: int) =
        async {
            let! channel =
                select {
                    for row in channels do
                        where (row.channel_id = channelId)
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapToModel |> Seq.tryExactlyOne
        }

    let add (channel: NewChannel) =
        async {
            let newChannel = {
                channel_id = int channel.ChannelId
                channel_name = channel.ChannelName
            }

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
