namespace IRC

type Request =
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

module Request =

    let private formatPrivMsg (message: string) =
        if message.Length > 500 then
            message[0..496] + "..."
        else
            message

    let capReq capabilities = CapReq capabilities
    let pass token = Pass token
    let nick username = Nick username

    let privMsg channel message =
        let message = message |> formatPrivMsg
        PrivMsg (channel, message)

    let replyMsg messageId channel  message = ReplyMsg (messageId, channel, message)
    let pong message  = Pong message
    let part channel  = Part channel
    let join channel  = Join channel
    let joinM channels  = JoinM channels
    let raw message = Raw message

    let toString command =
        match command with
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
