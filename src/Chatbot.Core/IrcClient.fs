namespace Clients

open System
open System.IO
open System.Net.Sockets
open System.Net.Security

open IRC.Commands

type IrcClient(host: string, port: int) =

    [<Literal>]
    let WriterBufferSize = 1024

    [<Literal>]
    let ReaderBufferSize = 10240

    let socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
    let mutable reader: TextReader = null
    let mutable writer: TextWriter = null

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
                Logging.error $"error in {nameof IO.readAsync}" ex
                return None
        }

    let writeLine message cancellationToken =
        async {
            try
                do! IO.writeLineAsync writer message cancellationToken
            with ex ->
                Logging.error $"error in {IO.writeLineAsync}" ex
        }

    let flush cancellationToken =
        async {
            try
                do! IO.flushAsync writer cancellationToken
            with ex ->
                Logging.error $"error in {IO.flushAsync}" ex
        }

    let send message cancellationToken =
        async {
            do! writeLine message cancellationToken
            do! flush cancellationToken
        }

    interface Clients.ITwitchConnection with
        member _.Connected = connected ()

        member _.ConnectAsync (cancellationToken) = connect cancellationToken

        member _.ReadAsync (cancellationToken) = read cancellationToken

        member _.SendAsync (message: string, cancellationToken) = send (message.AsMemory()) cancellationToken

        member _.AuthenticateAsync (user, accessToken, capabilities, cancellationToken) =
            async {
                if capabilities.Length > 0 then
                    do! writeLine ((CapReq capabilities).ToString().AsMemory()) cancellationToken

                do! writeLine ((Pass accessToken).ToString().AsMemory()) cancellationToken
                do! writeLine ((Nick user).ToString().AsMemory()) cancellationToken
            }

    interface IDisposable with
        member _.Dispose () =
            isConnected <- false
            writer.Dispose()
            socket.Dispose()
