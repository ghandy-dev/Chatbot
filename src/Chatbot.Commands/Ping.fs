namespace Chatbot.Commands

[<AutoOpen>]
module Ping =

    open System

    let private startTime =
        Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()

    let ping () =
        let duration = (DateTime.UtcNow - startTime)

        let timeOnline =
            sprintf "%d hours, %d minutes, %d seconds" (duration.TotalHours |> int) duration.Minutes duration.Seconds

        Ok <| Message $"Pong. Uptime: {timeOnline}"
