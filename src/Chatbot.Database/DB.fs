namespace Database

module internal DB =

    open Configuration
    open Database

    open System.Data
    open Microsoft.Data.Sqlite

    open Dapper
    open Dapper.FSharp.SQLite


    let connectionString = appConfig.ConnectionStrings.Database

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    let connection: IDbConnection =
        try
            let conn = new SqliteConnection(connectionString)
            conn.Open()
            conn
        with ex ->
            Logging.error ex.Message ex
            failwith (ex.Message)

    let users = table'<Entities.User> "users"
    let rpsStats = table'<Entities.RpsStats> "rps_stats"
    let channels = table'<Entities.Channel> "channels"
    let aliases = table'<Entities.Alias> "aliases"
    let reminders = table'<Entities.Reminder> "reminders"
