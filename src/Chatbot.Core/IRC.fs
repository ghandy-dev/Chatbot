namespace IRC

[<RequireQualifiedAccess>]
module IRC =

    type Command =
        | CapReq of capabilities: string list
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
            | CapReq capabilities -> sprintf """CAP REQ :%s""" (String.concat " " capabilities)
            | Pass token -> sprintf "PASS oauth:%s" token
            | Nick username -> sprintf "NICK %s" username
            | PrivMsg(channel, message) -> sprintf "PRIVMSG #%s :%s" channel message
            | ReplyMsg(messageId, channel, message) -> sprintf "@reply-parent-msg-id=%s PRIVMSG #%s :%s" messageId channel message
            | Pong message -> sprintf "PONG :%s" message
            | Part channel -> sprintf "PART #%s" channel
            | Join channel -> sprintf "JOIN #%s" channel
            | JoinM channels -> channels |> Seq.map (sprintf "#%s") |> String.concat "," |> sprintf "JOIN %s"
            | Raw message -> message
