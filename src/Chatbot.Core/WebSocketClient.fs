namespace Chatbot

open Chatbot.IRC

open System
open System.Net.WebSockets
open System.Threading

type WebSocketClient(host: string, port: int) =

    let [<Literal>] readerBufferSize = 10240

    let client = new ClientWebSocket()

    let connected () = client.State = WebSocketState.Open

    let connect cancellationToken =
        async {
            let uri = new Uri($"wss://{host}:{port}")
            do! client.ConnectAsync(uri, cancellationToken) |> Async.AwaitTask
        }

    let read cancellationToken =
        async {
            let buffer = new ArraySegment<byte>(Buffers.ArrayPool<byte>.Shared.Rent(readerBufferSize))
            let mutable count = 0

            let rec read' () =
                async {
                    let! (result: WebSocketReceiveResult) = client.ReceiveAsync(buffer, cancellationToken) |> Async.AwaitTask
                    if not <| result.EndOfMessage then
                        do! read' ()

                    count <- result.Count
                }

            do! read' ()

            let message = System.Text.Encoding.UTF8.GetString(buffer.Array, 0, count)
            if (not <| String.IsNullOrWhiteSpace(message)) then
                return Some message
            else
                return None
        }

    let writeLine (message: string) =
        async {
            let cancellationToken = new CancellationToken()
            try
                let bytes = message |> System.Text.Encoding.UTF8.GetBytes
                let buffer = bytes.AsMemory()

                do! client.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).AsTask() |> Async.AwaitTask
            with
            | :? ObjectDisposedException as ex -> Logging.error "error in writeLineAsync" ex |> ignore
            | :? InvalidOperationException as ex -> Logging.error "error in writeLineAsync" ex |> ignore
            | ex -> Logging.error "error in writeLineAsync" ex |> ignore
        }

    interface ITwitchConnection with
        member _.Connected = connected ()

        member _.ConnectAsync(cancellationToken) = connect cancellationToken

        member _.PongAsync (message) =
            async {
                let pongMessage = IRC.PongMessage message
                do! writeLine (pongMessage)
            }

        member _.PartChannelAsync (channel) =
            async {
                let partMessage = IRC.PartMessage channel
                do! writeLine (partMessage)
            }

        member _.JoinChannelAsync (channel) =
            async {
                let joinMessage = IRC.JoinMessage channel
                do! writeLine joinMessage
            }

        member _.JoinChannelsAsync (channels) =
            async {
                let joinMessage = IRC.JoinMultipleMessage channels
                do! writeLine joinMessage
            }

        member _.SendPrivMessageAsync (channel, message) =
            async {
                let privMessage = IRC.PrivMessage channel message
                do! writeLine privMessage
            }

        member _.ReadAsync (cancellationToken) = read cancellationToken

        member _.SendAsync (message: string) = writeLine message

        member _.AuthenticateAsync (user: string, accessToken: string, capabilities: string array) =
            async {
                if (capabilities.Length > 0) then
                    do! writeLine($"""CAP REQ :{String.concat " " capabilities}""")

                do! writeLine($"PASS oauth:{accessToken}")
                do! writeLine($"NICK {user}")
            }

    interface IDisposable with
        member _.Dispose () =
            client.Dispose()
