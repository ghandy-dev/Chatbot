namespace Database

module DB =

    open System.Data
    open Microsoft.Data.Sqlite

    open Dapper
    open Dapper.FSharp.SQLite

    open Database

    let private connectionString = Configuration.ConnectionStrings.config.Database

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    let internal connection: IDbConnection =
        try
            let conn = new SqliteConnection(connectionString)
            conn.Open()
            conn
        with ex ->
            Logging.error ex.Message ex
            failwith (ex.Message)

    let internal users = table'<Entities.User> "users"
    let internal rpsStats = table'<Entities.RpsStats> "rps_stats"
    let internal channels = table'<Entities.Channel> "channels"
    let internal aliases = table'<Entities.Alias> "aliases"
    let internal dungeon = table'<Entities.DungeonPlayer> "dungeon"
    let internal reminders = table'<Entities.Reminder> "reminders"
