namespace Chatbot.Commands

[<AutoOpen>]
module Ping =

    open System

    let private startTime = Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
    let private platform = Environment.OSVersion.Platform
    let private platformVersion = Environment.OSVersion.Version
    let private processors = Environment.ProcessorCount
    let private architecture = Runtime.InteropServices.RuntimeInformation.OSArchitecture
    let private version = Environment.Version

    let ping () =
        let duration = DateTime.UtcNow - startTime
        let timeOnline =
            $"{duration.TotalHours |> int} hours, {duration.Minutes} minutes, {duration.Seconds} seconds"

        Message
            $"Pong. Uptime: {timeOnline}. Running on platform: {platform} {platformVersion}, processors: {processors}, architecture: {architecture}, dotnet version: {version}."
