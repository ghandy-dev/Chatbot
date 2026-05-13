namespace Chatbot.Database.Repositories

[<RequireQualifiedAccess>]
module Channels =

    open Microsoft.Data.Sqlite

    open Dapper.FSharp.SQLite

    open Chatbot.Core.Domain.Types
    open Chatbot.Database.Db
    open Chatbot.Database.DbModels

    let private toChannel (dbChannel: DbChannel) : Channel =
        {
            ChannelId = dbChannel.channel_id
            ChannelName = dbChannel.channel_name
        }

    let getAll (db: Database) =
        async {
            use connection = new SqliteConnection(db.ConnectionString)
            connection.Open()

            let! channel =
                select {
                    for row in channels do
                        selectAll
                }
                |> connection.SelectAsync<DbChannel>
                |> Async.AwaitTask

            return
                channel
                |> Seq.map toChannel
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
                |> connection.SelectAsync<DbChannel>
                |> Async.AwaitTask

            return
                channel
                |> Seq.map toChannel
                |> Seq.tryExactlyOne
        }

    let add (db: Database) (newChannel: NewChannel) =
        async {
            let channel =
                {
                    channel_id = int newChannel.ChannelId
                    channel_name = newChannel.ChannelName
                }

            try
                use connection = new SqliteConnection(db.ConnectionString)
                connection.Open()

                let! rowsAffected =
                    insert {
                        into channels
                        value channel
                    }
                    |> connection.InsertAsync
                    |> Async.AwaitTask

                return Ok rowsAffected
            with ex ->
                return Error ex
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

                return Ok rowsAffected
            with ex ->
                return Error ex
        }

    let create db =

        {
            new IChannelRepository with
                member _.Add channel = add db channel
                member _.Delete channelId = delete db channelId
                member _.Get channelId =  get db channelId
                member _.GetAll() = getAll db
        }
