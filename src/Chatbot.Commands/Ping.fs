namespace Commands

[<AutoOpen>]
module Ping =

    open System

    open FsToolkit.ErrorHandling

    let private startTime = Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
    let private platform = Environment.OSVersion.Platform
    let private platformVersion = Environment.OSVersion.Version
    let private processors = Environment.ProcessorCount
    let private architecture = Runtime.InteropServices.RuntimeInformation.OSArchitecture
    let private version = Environment.Version

    let ping _ =
        result {
            let duration = DateTime.UtcNow - startTime
            let timeOnline = $"{duration.TotalHours |> int} hours, {duration.Minutes} minutes, {duration.Seconds} seconds"

            return Message $"Pong. Uptime: {timeOnline}. Running on platform: {platform} {platformVersion}, processors: {processors}, architecture: {architecture}, dotnet version: {version}."
        }
