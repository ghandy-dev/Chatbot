namespace Database

module internal DB =

    open System.Data

    open Dapper
    open Dapper.FSharp.SQLite
    open Microsoft.Data.Sqlite

    open Configuration
    open Database

    let connectionString = appConfig.ConnectionStrings.Database

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    let connection: IDbConnection =
        try
            let conn = new SqliteConnection(connectionString)
            conn.Open()
            conn
        with ex ->
            Logging.errorEx ex.Message ex
            reraise()

    let users = table'<Entities.User> "users"
    let rpsStats = table'<Entities.RpsStats> "rps_stats"
    let channels = table'<Entities.Channel> "channels"
    let aliases = table'<Entities.Alias> "aliases"
    let reminders = table'<Entities.Reminder> "reminders"
