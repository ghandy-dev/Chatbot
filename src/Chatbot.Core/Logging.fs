[<RequireQualifiedAccess>]
module Chatbot.Logging

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
type LogLevel =
    | Trace = 0
    | Debug = 1
    | Info = 2
    | Warning = 3
    | Error = 4
    | Critical = 5
    | None = 6

let private print value = Console.Write($"{value}")

let private printLine value = Console.WriteLine($"{value}")

let private logLevels =
    [
        LogLevel.Trace, ("TRACE", ConsoleColor.Cyan, None)
        LogLevel.Debug, ("DEBUG", ConsoleColor.Yellow, None)
        LogLevel.Info, ("INFO", ConsoleColor.Green, None)
        LogLevel.Warning, ("WARNING", ConsoleColor.DarkYellow, None)
        LogLevel.Error, ("ERROR", ConsoleColor.Red, None)
        LogLevel.Critical, ("CRITICAL", ConsoleColor.White, Some ConsoleColor.Red)
    ]
    |> Map.ofList

let private parseLogLevel logLevel =
    match logLevel with
    | "Trace" -> LogLevel.Trace
    | "Debug" -> LogLevel.Debug
    | "Information" -> LogLevel.Info
    | "Warning" -> LogLevel.Warning
    | "Error" -> LogLevel.Error
    | "Critical" -> LogLevel.Critical
    | _ -> failwithf "Unknown Log Level: %s" logLevel

type Logger(name: string, ?logLevel: LogLevel) =

    let logLevel =
        match logLevel with
        | Some level -> level
        | None -> Configuration.Logging.config.LogLevel.Default |> parseLogLevel

    member _.Log (logLevel, msg: string, ?ex: Exception) =
        let time = DateTime.UtcNow.ToString("HH:mm:ss")
        let (label, foregroundColor, backgroundColor) = logLevels[logLevel]

        match backgroundColor with
        | None -> ()
        | Some color -> Console.BackgroundColor <- color

        print $"{time} [{name}] "
        Console.ForegroundColor <- foregroundColor
        print $"[{label}]"
        Console.ResetColor()
        print " "

        match ex with
        | Some ex ->
            printLine $"{msg}"
            printLine $"Exception message: {ex.Message}"
            printLine $"Stack trace: {ex.StackTrace}"
        | None -> printLine $"{msg}"

    member this.LogDebug (msg: string) =
        if logLevel <= LogLevel.Debug then
            this.Log(LogLevel.Debug, msg)

    member this.LogTrace (msg: string) =
        if logLevel <= LogLevel.Trace then
            this.Log(LogLevel.Trace, msg)

    member this.LogInfo (msg: string) =
        if logLevel <= LogLevel.Info then
            this.Log(LogLevel.Info, msg)

    member this.LogWarning (msg: string) =
        if logLevel <= LogLevel.Warning then
            this.Log(LogLevel.Warning, msg)

    member this.LogError (msg: string, ?ex: Exception) =
        if logLevel <= LogLevel.Error then
            this.Log(LogLevel.Error, msg, Option.toObj ex)

    member this.LogCritical (msg: string, ?ex: Exception) =
        if logLevel <= LogLevel.Critical then
            this.Log(LogLevel.Critical, msg, Option.toObj ex)

let private loggers = new Dictionary<string, Logger>()

let private getLogger name =
    if loggers.ContainsKey(name) then
        loggers[name]
    else
        let logger = Logger(name)
        loggers.Add(name, logger)
        logger

let createNamedLogger name = getLogger name

let createLogger<'a> = createNamedLogger (typeof<'a>.Name)
