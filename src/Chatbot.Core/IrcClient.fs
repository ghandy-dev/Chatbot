namespace IRC

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
    let mutable reader: TextReader = null
    let mutable writer: TextWriter = null

    let mutable isConnected = true

    let connected () =
        if isConnected = false then false else socket.Connected

    let connect cancellationToken =
        async {
            do! socket.ConnectAsync(host, port, cancellationToken).AsTask() |> Async.AwaitTask
            let stream = new NetworkStream(socket)
            let sslStream = new SslStream(stream)
            sslStream.AuthenticateAsClient(host)
            reader <- IO.createStreamReader sslStream
            writer <- IO.createStreamWriter sslStream WriterBufferSize
        }

    let read cancellationToken =
        IO.readAsync reader ReaderBufferSize cancellationToken

    let writeLine (message: string) =
        async {
            try
                do! IO.writeLineAsync writer message
            with
            | :? ObjectDisposedException as ex -> Logging.error "error in writeLineAsync" ex |> ignore
            | :? InvalidOperationException as ex -> Logging.error "error in writeLineAsync" ex |> ignore
            | ex -> Logging.error "error in writeLineAsync" ex |> ignore
        }

    let flush () =
        async {
            try
                do! IO.flushAsync writer
            with
            | :? ObjectDisposedException as ex -> Logging.error "error in flushAsync" ex |> ignore
            | :? InvalidOperationException as ex -> Logging.error "error in flushAsync" ex |> ignore
            | ex -> Logging.error "error in flushAsync" ex |> ignore
        }

    let send (message: string) =
        async {
            do! writeLine (message)
            do! flush ()
        }

    interface Clients.ITwitchConnection with
        member _.Connected = connected ()

        member _.ConnectAsync (cancellationToken) = connect cancellationToken

        member _.ReadAsync (cancellationToken) = read cancellationToken

        member _.SendAsync (message) = send message

        member _.AuthenticateAsync (user: string, accessToken: string, capabilities: string array) =
            async {
                if (capabilities.Length > 0) then
                    do! writeLine ($"""CAP REQ :{String.concat " " capabilities}""")

                do! writeLine ($"PASS oauth:{accessToken}")
                do! writeLine ($"NICK {user}")
                do! flush ()
            }

    interface IDisposable with
        member _.Dispose () =
            isConnected <- false
            writer.Dispose()
            socket.Dispose()
