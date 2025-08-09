namespace IRC

module Parsing =

    module Types =

        type IrcCommand =
            // IRC Commands
            | Authenticated
            | Cap
            | Join
            | Part
            | Ping
            | PrivMsg
            | RoomList
            | Unsupported
            // Twitch IRC Commands
            | ClearChat
            | ClearMsg
            | GlobalUserState
            | HostTarget
            | Notice
            | Reconnect
            | RoomState
            | UserNotice
            | UserState
            | Whisper
            // ignore
            | Ignore
            | Unknown

            static member parse =
                function
                | "001" -> Authenticated
                | "CAP" -> Cap
                | "CLEARCHAT" -> ClearChat
                | "CLEARMSG" -> ClearMsg
                | "JOIN" -> Join
                | "GLOBALUSERSTATE" -> GlobalUserState
                | "HOSTTARGET" -> HostTarget
                | "NOTICE" -> Notice
                | "PART" -> Part
                | "PING" -> Ping
                | "PRIVMSG" -> PrivMsg
                | "RECONNECT" -> Reconnect
                | "353" -> RoomList
                | "ROOMSTATE" -> RoomState
                | "USERNOTICE" -> UserNotice
                | "USERSTATE" -> UserState
                | "WHISPER" -> Whisper
                | "421" -> Unsupported
                | "002"
                | "003"
                | "004"
                | "366"
                | "372"
                | "375"
                | "376" -> Ignore
                | _ -> Unknown

        type Source = {
            Nick: string
            Host: string option
        }

        type IrcMessage = {
            Tags: Map<string, string>
            Source: Source option
            Command: IrcCommand
            Parameters: string
        } with

            static member empty = {
                Tags = Map.empty
                Source = None
                Command = Unknown
                Parameters = ""
            }

    [<RequireQualifiedAccess>]
    module Parser =

        open Types

        let private parseTags (message: string) =
            message.Split(";")
            |> Array.fold
                (fun (m: Map<_, _>) s ->
                    let kv = s.Split("=", 2)
                    m.Add(kv[0], kv[1])
                )
                Map.empty

        let private parseSource (message: string) =
            message.Split("!")
            |> function
                | [| nick |] ->
                    Some {
                        Nick = nick
                        Host = None
                    }
                | [| nick ; host |] ->
                    Some {
                        Nick = nick
                        Host = Some host
                    }
                | _ -> None

        type private ParseComponent =
            | ParseTags
            | ParseSource
            | ParseCommand
            | ParseParameters
            | MessageParsed

        let private parseMessageComponents (message: string) =
            let rec parseComponents (parts: string array) (next: ParseComponent) (parsedMessage: IrcMessage) =
                match next with
                | ParseTags ->
                    if parts[0].StartsWith('@') then
                        let tags = parseTags parts.[0].[1..]
                        parseComponents parts[1..] ParseSource { parsedMessage with Tags = tags }
                    else
                        parseComponents parts ParseSource parsedMessage
                | ParseSource ->
                    if parts[0].StartsWith(':') then
                        let source = parseSource parts.[0].[1..]
                        parseComponents parts[1..] ParseCommand { parsedMessage with Source = source }
                    else
                        parseComponents parts ParseCommand parsedMessage
                | ParseCommand ->
                    match parts with
                    | [||] -> parseComponents parts MessageParsed parsedMessage
                    | parts ->
                        let command = IrcCommand.parse parts[0]

                        let parsedMessage = { parsedMessage with Command = command }

                        if parts.Length <= 1 then
                            parseComponents parts MessageParsed parsedMessage
                        else
                            parseComponents parts[1..] ParseParameters parsedMessage
                | ParseParameters ->
                    let parameters = parts[0]
                    parseComponents parts MessageParsed { parsedMessage with Parameters = parameters }
                | MessageParsed -> parsedMessage

            let parts =
                if message[0] = '@' then
                    message.Split(" ", 4)
                else
                    message.Split(" ", 3)

            parseComponents parts ParseTags IrcMessage.empty

        let parseIrcMessages (messages: string array) = messages |> Array.map parseMessageComponents
