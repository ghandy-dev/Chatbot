namespace IRC

module Commands =

    type Command =
        | CapReq of capabilities: string seq
        | Pass of token: string
        | Nick of username: string
        | PrivMsg of channel: string * message: string
        | ReplyMsg of messageId: string * channel: string * message: string
        | Pong of message: string
        | Part of channel: string
        | Join of channel: string
        | JoinM of channels: string seq
        | Raw of string

        override this.ToString () =
            match this with
            | CapReq capabilities -> $"""CAP REQ :%s{String.concat " " capabilities}"""
            | Pass token -> $"PASS oauth:%s{token}"
            | Nick username -> $"NICK %s{username}"
            | PrivMsg(channel, message) -> $"PRIVMSG #%s{channel} :%s{message}"
            | ReplyMsg(messageId, channel, message) -> $"@reply-parent-msg-id=%s{messageId} PRIVMSG #%s{channel} :%s{message}"
            | Pong message -> $"PONG :%s{message}"
            | Part channel -> $"PART #%s{channel}"
            | Join channel -> $"JOIN #%s{channel}"
            | JoinM channels -> channels |> Seq.map (sprintf "#%s") |> String.concat "," |> sprintf "JOIN %s"
            | Raw message -> message
