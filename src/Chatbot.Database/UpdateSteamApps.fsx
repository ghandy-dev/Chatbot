#r "nuget: FsHttp, 10.0.0"
#r "nuget: Microsoft.Data.Sqlite, 7.0.2"

open FsHttp
open System
open System.Data
open Microsoft.Data.Sqlite

type SteamAppEntity = { app_id: int; name: string }

type SteamApp = { appId: int; name: string }

type AppList = { apps: SteamApp list }

type GetAppListResponse = { applist: AppList }

[<Literal>]
let connectionString =
    "Data Source="
    + __SOURCE_DIRECTORY__
    + @"\\chat.db;"

let connection: IDbConnection = new SqliteConnection(connectionString)

let response =
    http {
        GET("https://api.steampowered.com/ISteamApps/GetAppList/v2")
        CacheControl "no-cache"
        Accept MimeTypes.applicationJson
    }
    |> Request.send
    |> Response.deserializeJson<GetAppListResponse>

let apps =
    response.applist.apps
    |> List.choose (fun x ->
        if not <| String.IsNullOrWhiteSpace(x.name) then
            Some { app_id = x.appId; name = x.name.Trim() }
        else
            None)
    |> List.countBy id
    |> List.map fst

printfn "rows to be inserted: %d" apps.Length

try
    connection.Open()

    let transaction = connection.BeginTransaction()

    let command = connection.CreateCommand()
    command.CommandText <- @"INSERT INTO [steam_apps] VALUES (@appId, @name)"

    let appIdParameter = command.CreateParameter()
    appIdParameter.ParameterName <- "@appId"

    let appNameParameter = command.CreateParameter()
    appNameParameter.ParameterName <- "@name"

    try
        command.Parameters.Add(appIdParameter) |> ignore
        command.Parameters.Add(appNameParameter) |> ignore

        for c in command.Parameters do
            printfn "%s %A" (c :?> IDbDataParameter).ParameterName (c :?> IDbDataParameter).Value

        let mutable count = 0

        apps
        |> List.iter (fun x ->
            appIdParameter.Value <- x.app_id
            appNameParameter.Value <- x.name
            let rowsAffected = command.ExecuteNonQuery()
            count <- count + rowsAffected)

        transaction.Commit()

        printfn "rows inserted: %d" count
    with
    | (ex: exn) ->
        printfn "%s\n%s" ex.Message ex.StackTrace
        transaction.Rollback()
finally
    connection.Close()
