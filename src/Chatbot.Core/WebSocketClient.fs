namespace Clients

open System
open System.Net.WebSockets

type WebSocketClient(host: string, port: int) =

    [<Literal>]
    let readerBufferSize = 10240

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
            let! result = client.ReceiveAsync(buffer, cancellationToken) |> Async.AwaitTask

            if result.Count > 0 then
                let message = System.Text.Encoding.UTF8.GetString(buffer.Array, 0, result.Count)
                return Some message
            else
                return None
        }

    let writeLine (message: string) cancellationToken =
        async {
            try
                let bytes = message |> System.Text.Encoding.UTF8.GetBytes
                let buffer = bytes.AsMemory()

                do! client.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).AsTask() |> Async.AwaitTask
            with ex ->
                Logging.errorEx $"error in writeLine" ex
        }

    interface IConnection with
        member _.Connected = connected ()

        member _.ConnectAsync(cancellationToken) = connect cancellationToken

        member _.ReadAsync (cancellationToken) = read cancellationToken

        member _.SendAsync (message, cancellationToken) = writeLine message cancellationToken

    interface IDisposable with
        member _.Dispose () =
            client.Dispose()
