[<RequireQualifiedAccess>]
module Logging

open Configuration

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
type LogLevel =
    | Trace
    | Debug
    | Info
    | Warning
    | Error
    | Critical

    member x.toInt () =
        x
        |> function
            | Trace -> 1
            | Debug -> 2
            | Info -> 3
            | Warning -> 4
            | Error -> 5
            | Critical -> 6

    override x.ToString () =
        x
        |> function
            | Trace -> "Trace"
            | Debug -> "Debug"
            | Info -> "Info"
            | Warning -> "Warning"
            | Error -> "Error"
            | Critical -> "Critical"

type LogEntry = {
    Timestamp: DateTime
    Level: LogLevel
    Message: string
    Exception: Exception option
}

let private parseLogLevel logLevel =
    match logLevel with
    | "Trace" -> LogLevel.Trace
    | "Debug" -> LogLevel.Debug
    | "Information" -> LogLevel.Info
    | "Warning" -> LogLevel.Warning
    | "Error" -> LogLevel.Error
    | "Critical" -> LogLevel.Critical
    | _ -> failwithf "Unknown Log Level: %s" logLevel

let private defaultLogLevel = LogLevel.Info

let currentLogLevel = parseLogLevel appConfig.Logging.LogLevel.Default

let private toColor logLevel =
    match logLevel with
    | LogLevel.Trace -> ConsoleColor.Cyan, None
    | LogLevel.Debug -> ConsoleColor.Yellow, None
    | LogLevel.Info -> ConsoleColor.Green, None
    | LogLevel.Warning -> ConsoleColor.DarkYellow, None
    | LogLevel.Error -> ConsoleColor.Red, None
    | LogLevel.Critical -> ConsoleColor.White, Some ConsoleColor.Red

let private shouldLog (logLevel: LogLevel) =
    match currentLogLevel with
    | LogLevel.Trace -> true
    | LogLevel.Debug -> logLevel >= LogLevel.Debug
    | LogLevel.Info -> logLevel >= LogLevel.Info
    | LogLevel.Warning -> logLevel >= LogLevel.Warning
    | LogLevel.Error -> logLevel >= LogLevel.Error
    | LogLevel.Critical -> logLevel >= LogLevel.Critical

let private logEntry entry =
    let foregroundColor, backgroundColor = toColor entry.Level

    printf "[%s] " (entry.Timestamp.ToString("yyyy/MM/dd HH:mm:ss"))
    Console.ForegroundColor <- foregroundColor
    match backgroundColor with
    | None -> ()
    | Some color -> Console.BackgroundColor <- color
    printf "[%A]: " entry.Level
    Console.ResetColor()
    printfn "%s" entry.Message

    match entry.Exception with
    | None -> ()
    | Some ex -> printfn "%s" (ex.ToString())

let private log logLevel message ex =
    let entry = {
        Timestamp = DateTime.UtcNow
        Level = logLevel
        Message = message
        Exception = ex
    }

    match shouldLog logLevel with
    | true -> logEntry entry
    | false -> ()

let trace msg = log LogLevel.Trace msg None
let debug msg = log LogLevel.Debug msg None
let info msg = log LogLevel.Info msg None
let warning msg = log LogLevel.Warning msg None
let error msg ex = log LogLevel.Error msg (Some ex)
let critical msg ex = log LogLevel.Critical msg (Some ex)
