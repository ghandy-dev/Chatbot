namespace Chatbot.IRC

[<RequireQualifiedAccess>]
module IRC =

    open System.IO
    open System.Net.Security
    open System.Net.Sockets

    [<Literal>]
    let writeBufferSize = 1024

    [<Literal>]
    let readBufferSize = 16384 // 1024 * 16

    let private ircPrivMessage channel message = $"PRIVMSG #{channel} :{message}"
    let private ircPongMessage message = $"PONG :{message}"
    let private ircPartMessage channel = $"PART #{channel}"
    let private ircJoinMessage channel = $"JOIN #{channel}"

    let createTcpClient (host: string) (port: int) =
        let s = new Socket(SocketType.Stream, ProtocolType.Tcp)
        s.ConnectAsync(host, port) |> Async.AwaitTask |> Async.RunSynchronously
        new NetworkStream(s)

    let getSslStream (client: NetworkStream) (host: string) =
        let sslStream = new SslStream(client)
        sslStream.AuthenticateAsClient(host)
        sslStream

    let ircSendPrivMessage (writer: TextWriter) channel message =
        async { do! writer.WriteLineAsync(ircPrivMessage channel message) |> Async.AwaitTask }

    let ircPong (writer: TextWriter) message =
        async { do! writer.WriteLineAsync(ircPongMessage message) |> Async.AwaitTask }

    let ircPartChannel (writer: TextWriter) channel =
        async { do! writer.WriteLineAsync(ircPartMessage channel) |> Async.AwaitTask }

    let ircJoinChannel (writer: TextWriter) channel =
        async { do! writer.WriteLineAsync(ircJoinMessage channel) |> Async.AwaitTask }

    let ircJoinChannels (writer: TextWriter) channels =
        let channels = channels |> List.map (fun c -> $"#{c}") |> String.concat ","

        async { do! writer.WriteLineAsync($"JOIN {channels}") |> Async.AwaitTask }

open Chatbot

open System

type IrcClient(host: string, port: int) =

    [<Literal>]
    let writerBufferSize = 1024

    [<Literal>]
    let readerufferSize = 10240

    let logger = Logging.createLogger<IrcClient> None
    let client = IRC.createTcpClient host port
    let stream = IRC.getSslStream client host
    let reader = IO.createStreamReader stream
    let writer = IO.createStreamWriter stream writerBufferSize

    let mutable isConnected = true

    let connected () =
        if isConnected = false then
            false
        else
            client.Socket.Connected

    let readAsync cancellationToken =
        IO.readAsync reader readerufferSize cancellationToken

    let writeLineAsync (message: string) =
        async {
            try
                do! IO.writeLineAsync writer message
            with
            | :? ObjectDisposedException as ex -> logger.LogError("error in writeLineAsync", ex) |> ignore
            | :? InvalidOperationException as ex -> logger.LogError("error in writeLineAsync", ex) |> ignore
            | ex -> logger.LogError("error in writeLineAsync", ex) |> ignore
        }

    let flushAsync () =
        async {
            try
                do! IO.flushAsync writer
            with
            | :? ObjectDisposedException as ex -> logger.LogError("error in flushAsync", ex) |> ignore
            | :? InvalidOperationException as ex -> logger.LogError("error in flushAsync", ex) |> ignore
            | ex -> logger.LogError("error in flushAsync", ex) |> ignore
        }

    let sendAsync (message: string) =
        async {
            do! writeLineAsync (message)
            do! flushAsync ()
        }

    member _.Connected = connected ()

    member _.PongAsync (message) =
        async {
            do! IRC.ircPong writer message
            do! flushAsync ()
        }

    member _.PartChannel (channel) =
        async {
            do! IRC.ircPartChannel writer channel
            do! flushAsync ()
        }

    member _.JoinChannel (channel) =
        async {
            do! IRC.ircJoinChannel writer channel
            do! flushAsync ()
        }

    member _.JoinChannels (channels) =
        async {
            do! IRC.ircJoinChannels writer channels
            do! flushAsync ()
        }

    member _.SendPrivMessage (channel, message) =
        async {
            do! IRC.ircSendPrivMessage writer channel message
            do! flushAsync ()
        }

    member _.ReadAsync (cancellationToken) = readAsync cancellationToken

    member _.WriteAsync (message: string) = writeLineAsync message

    member _.SendAsync (message: string) = sendAsync message

    member _.FlushAsync () = flushAsync ()

    interface IDisposable with
        member _.Dispose () =
            isConnected <- false
            writer.Dispose()
            client.Dispose()
