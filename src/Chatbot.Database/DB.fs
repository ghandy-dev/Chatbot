namespace Chatbot.Database

module DB =

    open System.Data
    open Microsoft.Data.Sqlite

    open Dapper
    open Dapper.FSharp.SQLite

    open Chatbot.Database

    let private connectionString = Chatbot.Configuration.ConnectionStrings.config.Database

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    let internal connection: IDbConnection =
        let conn = new SqliteConnection(connectionString)
        conn.Open()
        conn

    let internal users = table'<Entities.User> "users"
    let internal rpsStats = table'<Entities.RpsStats> "rps_stats"
    let internal channels = table'<Entities.Channel> "channels"
    let internal aliases = table'<Entities.Alias> "aliases"
