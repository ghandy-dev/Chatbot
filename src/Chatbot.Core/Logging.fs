[<RequireQualifiedAccess>]
module Chatbot.Logging

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
type LogLevel =
    | Debug = 0
    | Trace = 1
    | Info = 2
    | Warning = 3
    | Error = 4

let private print value = Console.Write($"{value}")

let private printLine value = Console.WriteLine($"{value}")

let private logLevels =
    [
        LogLevel.Debug, ("DEBUG", ConsoleColor.Yellow)
        LogLevel.Trace, ("TRACE", ConsoleColor.Cyan)
        LogLevel.Info, ("INFO", ConsoleColor.Green)
        LogLevel.Warning, ("WARNING", ConsoleColor.DarkYellow)
        LogLevel.Error, ("ERROR", ConsoleColor.Red)
    ]
    |> Map.ofList

type Logger(name: string, ?logLevel: LogLevel) =

    let logLevel =
        match logLevel with
        | Some level -> level
        | None -> LogLevel.Info

    member _.Log (logLevel, msg: string, ?ex: Exception) =
        let time = DateTime.UtcNow.ToString("HH:mm:ss")
        let (label, foregroundColor) = logLevels[logLevel]

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

    member this.LogError (msg: string, ex: Exception) =
        if logLevel <= LogLevel.Error then
            this.Log(LogLevel.Error, msg, ex)

let private loggers = new Dictionary<string, Logger>()

let private getLogger name loglevel =
    if loggers.ContainsKey(name) then
        loggers[name]
    else
        let logger = Logger(name, loglevel)
        loggers.Add(name, logger)
        logger

let createLogger<'a> loglevel =
    match loglevel with
    | None -> getLogger (typeof<'a>).Name LogLevel.Info
    | Some level -> getLogger (typeof<'a>).Name level


let createNamedLogger name loglevel =
    match loglevel with
    | None -> getLogger name LogLevel.Info
    | Some level -> getLogger name level
