namespace IRC

[<RequireQualifiedAccess>]
module IRC =

    type Command =
        | CapReq of capabilities: string list
        | Pass of token: string
        | Nick of username: string
        | PrivMsg of channel: string * message: string
        | Pong of message: string
        | Part of channel: string
        | Join of channel: string
        | JoinM of channels: string seq
        | Raw of string

        static member ToString =
            function
            | CapReq(capabilities) -> $"""CAP REQ :{String.concat " " capabilities}"""
            | Pass token -> $"PASS oauth:{token}"
            | Nick username -> $"NICK {username}"
            | PrivMsg(channel, message) -> $"PRIVMSG #{channel} :{message}"
            | Pong message -> $"PONG :{message}"
            | Part channel -> $"PART #{channel}"
            | Join channel -> $"JOIN #{channel}"
            | JoinM channels -> channels |> Seq.map (fun c -> $"#{c}") |> String.concat "," |> (fun cs -> $"JOIN {cs}")
            | Raw message -> message
