module Chatbot.Database.Migration

type AssemblyMarker = class end

open System
open System.IO
open System.Text.RegularExpressions

open Microsoft.Data.Sqlite

open Dapper

open Chatbot.Common
open Chatbot.Database.Db

let private versionRegex = new Regex("^\d{3}", RegexOptions.Compiled)

let run (db: Database) =
    use connection = new SqliteConnection(db.ConnectionString)

    connection.Open()

    let _ = connection.Execute(
        """
        CREATE TABLE IF NOT EXISTS [migrations] (
            [version] INT PRIMARY KEY NOT NULL,
            [name] TEXT NOT NULL,
            [applied_at] TEXT NOT NULL
        ) WITHOUT ROWID;
        """
    )

    let dbMigrations =
        connection.Query<int>("SELECT version FROM [migrations]")
        |> Set.ofSeq

    let assembly = typeof<AssemblyMarker>.Assembly
    let assemblyName = assembly.GetName().Name

    let resources =
        assembly.GetManifestResourceNames()
        |> Array.filter _.EndsWith(".sql")

    try
        for resource in resources do
            use stream = new StreamReader(assembly.GetManifestResourceStream(resource))
            let file = resource |> String.replace $"{assemblyName}.Migrations." ""

            let versionRegex = versionRegex.Match(file)

            if versionRegex.Success then
                let version = versionRegex.Groups[0].Value |> Int32.Parse

                if not (dbMigrations |> Set.contains version) then
                    printfn "Applying migration %d %s" version file

                    let content = stream.ReadToEnd()
                    use transaction = connection.BeginTransaction()

                    let _ = connection.Execute(content)
                    let _ = connection.Execute(
                        """
                            INSERT INTO
                                [migrations]
                                (version, name, applied_at)
                            VALUES
                                (@version, @name, @applied_at)
                        """,
                        {| version = version ; name = file ; applied_at = utcNow() |})

                    transaction.Commit()
            else
                raise (new exn("Failed to read script version number"))
    with ex ->
        printfn "Migration failed:"
        printfn "%A" ex
        raise ex