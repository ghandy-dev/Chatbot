namespace Chatbot.IRC

[<RequireQualifiedAccess>]
module IRC =

    open System.IO
    open System.Net.Security
    open System.Net.Sockets


    let [<Literal>] writeBufferSize = 1024
    let [<Literal>] readBufferSize = 16384 // 1024 * 16

    let private ircPrivMessage channel message = $"PRIVMSG #{channel} :{message}"
    let private ircPongMessage message = $"PONG :{message}"
    let private ircPartMessage channel = $"PART #{channel}"
    let private ircJoinMessage (channel: 'a) = $"JOIN #{channel}"

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
