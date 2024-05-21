namespace Chatbot.Database

module DB =

    open System.Data
    open Microsoft.Data.Sqlite

    open Dapper
    open Dapper.FSharp
    open Dapper.FSharp.SQLite

    open Chatbot.Database

    [<Literal>]
    let private connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\chat.db;Version=3;"

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    let internal connection: IDbConnection =
        let conn = new SqliteConnection(connectionString)
        conn

    let internal users = table'<Entities.User> "users"
    let internal rpsStats = table'<Entities.RpsStats> "rps_stats"
    let internal channels = table'<Entities.Channel> "channels"
