module Shared

open Database.Types.Users

open System
open System.Collections.Concurrent
open System.Text


let userCommandCooldowns = new ConcurrentDictionary<(User * string), DateTime>()
let userStates = new ConcurrentDictionary<string, UserState>()
let channelStates = new ConcurrentDictionary<string, RoomState>()

let formatChatMessage (message: string) =
    let message =
        if message.Length > 500 then
            message[0..494] + "..."
        else
            message

    let maybeIndex =
        message
        |> Seq.indexed
        |> Seq.filter (fun (_, c) -> c = ' ')
        |> Seq.tryRandomChoice

    match maybeIndex with
    | None -> message
    | Some (index, _) ->
        let sb = new StringBuilder()
        let span = message.AsSpan()
        sb.Append(span.Slice(0, index)) |> ignore
        sb.Append(" \U000e0000") |> ignore
        sb.Append(span.Slice(index, message.Length - index)) |> ignore
        sb.ToString()