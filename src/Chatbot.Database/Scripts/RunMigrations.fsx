#r @"../bin/Debug/net10.0/Chatbot.Database.dll"
#r "nuget: FSharpPlus, 1.8.0"
#r "nuget: Microsoft.Recognizers.Text.DateTime, 1.8.13"
#r "nuget: Microsoft.Data.Sqlite, 10.0.0.0"
#r "nuget: Dapper, 2.0.0.0"
#r "nuget: Dapper.FSharp, 4.9.0"

open System
open System.IO

open Chatbot.Database
open Chatbot.Database.Db

let dbPath = @"Data Source=D:\Documents\Projects\Twitch\ChatBot\data\test.db"
let db = { ConnectionString = dbPath }

// let path = Path.GetFullPath(dbPath)
// printfn "%A" path

Directory.SetCurrentDirectory("../")

Migration.run db |> Async.RunSynchronously