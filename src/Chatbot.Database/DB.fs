namespace Database

module DB =

    open Dapper
    open Dapper.FSharp.SQLite

    open Database

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    type Database = {
        ConnectionString: string
    }

    let create connectionString = {
        ConnectionString = connectionString
    }

    let users = table'<Entities.User> "users"
    let rpsStats = table'<Entities.RpsStats> "rps_stats"
    let channels = table'<Entities.Channel> "channels"
    let aliases = table'<Entities.Alias> "aliases"
    let reminders = table'<Entities.Reminder> "reminders"
