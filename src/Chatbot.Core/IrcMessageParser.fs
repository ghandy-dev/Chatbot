namespace IRC

type Source = {
    Nick: string
    Host: string option
}

type MessageData = {
    Tags: Map<string, string>
    Source: Source option
    Command: string
    Parameters: string
}

module MessageData =

    let empty = {
        Tags = Map.empty
        Source = None
        Command = ""
        Parameters = ""
    }

module Parsing =

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
        let rec parseComponents (parts: string array) (next: ParseComponent) (parsedMessage: MessageData) =
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
                    let command = parts[0]
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

        parseComponents parts ParseTags MessageData.empty

    let parseRaw = parseMessageComponents
