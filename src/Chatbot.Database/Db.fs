namespace Chatbot.Database

module Db =

    open Dapper
    open Dapper.FSharp.SQLite

    open Chatbot.Database

    OptionTypes.register ()
    DefaultTypeMap.MatchNamesWithUnderscores <- true

    type Database = {
        ConnectionString: string
    }

    let create connectionString = {
        ConnectionString = connectionString
    }

    let internal users = table'<Entities.User> "users"
    let internal rpsStats = table'<Entities.RpsStats> "rps_stats"
    let internal channels = table'<Entities.Channel> "channels"
    let internal aliases = table'<Entities.Alias> "aliases"
    let internal reminders = table'<Entities.Reminder> "reminders"
