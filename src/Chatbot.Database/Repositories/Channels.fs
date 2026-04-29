namespace Chatbot.Database

module Channels =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Chatbot.Database.Models
    open Chatbot.Database.Entities
    open Db

    let mapToModel (channel: Entities.Channel) : Models.Channel = {
        ChannelId = string channel.channel_id
        ChannelName = channel.channel_name
    }

    let getAll (db: Database) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! channel =
                select {
                    for row: Channel in channels do
                        selectAll
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapToModel
        }

    let get (db: Database) (channelId: int) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! channel =
                select {
                    for row in channels do
                        where (row.channel_id = channelId)
                }
                |> connection.SelectAsync<Entities.Channel>
                |> Async.AwaitTask

            return channel |> Seq.map mapToModel |> Seq.tryExactlyOne
        }

    let add (db: Database) (channel: NewChannel) =
        async {
            let newChannel = {
                channel_id = int channel.ChannelId
                channel_name = channel.ChannelName
            }

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    insert {
                        into channels
                        value newChannel
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                return DatabaseResult.Failure
        }

    let delete (db: Database) (channelId: int) =
        async {
            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    delete {
                        for row in channels do
                            where (row.channel_id = channelId)
                    }
                    |> connection.DeleteAsync
                    |> Async.AwaitTask

                return DatabaseResult.Success rowsAffected
            with ex ->
                return DatabaseResult.Failure
        }
