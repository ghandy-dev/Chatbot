namespace Clients

open System
open System.IO
open System.Net.Sockets
open System.Net.Security

type IrcClient(host: string, port: int) =

    [<Literal>]
    let WriterBufferSize = 1024

    [<Literal>]
    let ReaderBufferSize = 10240

    let socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
    let mutable reader: StreamReader = null
    let mutable writer: StreamWriter = null

    let mutable isConnected = true

    let connected () = isConnected && socket.Connected

    let connect cancellationToken =
        async {
            try
                do! socket.ConnectAsync(host, port, cancellationToken).AsTask() |> Async.AwaitTask
                let stream = new NetworkStream(socket)
                let sslStream = new SslStream(stream)
                sslStream.AuthenticateAsClient(host)
                reader <- IO.createStreamReader sslStream
                writer <- IO.createStreamWriter sslStream WriterBufferSize
            with ex ->
                isConnected <- false
                raise ex
        }

    let read cancellationToken =
        async {
            try
                return! IO.readAsync reader ReaderBufferSize cancellationToken
            with ex ->
                Logging.errorEx $"error in {nameof IO.readAsync}" ex
                return None
        }

    let writeLine message cancellationToken =
        async {
            try
                do! IO.writeLineAsync writer message cancellationToken
            with ex ->
                Logging.errorEx $"error in {IO.writeLineAsync}" ex
        }

    let flush cancellationToken =
        async {
            try
                do! IO.flushAsync writer cancellationToken
            with ex ->
                Logging.errorEx $"error in {IO.flushAsync}" ex
        }

    let send message cancellationToken =
        async {
            do! writeLine message cancellationToken
            do! flush cancellationToken
        }

    interface Clients.IConnection with
        member _.Connected = connected ()

        member _.ConnectAsync (cancellationToken) = connect cancellationToken

        member _.ReadAsync (cancellationToken) = read cancellationToken

        member _.SendAsync (message: string, cancellationToken) = send (message.AsMemory()) cancellationToken

    interface IDisposable with
        member _.Dispose () =
            isConnected <- false
            writer.Dispose()
            socket.Dispose()
