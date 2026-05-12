module Chatbot.Database.Db

open Dapper.FSharp.SQLite

open Chatbot.Database.DbModels

OptionTypes.register ()

type Database = {
    ConnectionString: string
}

let create connectionString = {
    ConnectionString = connectionString
}

let internal users = table'<DbUser> "users"
let internal rpsStats = table'<DbRpsStats> "rps_stats"
let internal channels = table'<DbChannel> "channels"
let internal aliases = table'<DbAlias> "aliases"
let internal reminders = table'<DbReminder> "reminders"
