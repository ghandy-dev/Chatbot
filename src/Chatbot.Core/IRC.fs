namespace Chatbot.IRC

[<RequireQualifiedAccess>]
module IRC =

    open System.IO
    open System.Net.Security
    open System.Net.Sockets

    let PrivMessage channel message = $"PRIVMSG #{channel} :{message}"
    let PongMessage message = $"PONG :{message}"
    let PartMessage channel = $"PART #{channel}"
    let JoinMessage (channel: 'a) = $"JOIN #{channel}"
    let JoinMultipleMessage (channels: string seq) =
        channels
        |> Seq.map (fun c -> $"#{c}")
        |> String.concat ","
        |> fun cs -> $"JOIN {cs}"

    let [<Literal>] writeBufferSize = 1024
    let [<Literal>] readBufferSize = 16384 // 1024 * 16

    let createTcpClient (host: string) (port: int) =
        new Socket(SocketType.Stream, ProtocolType.Tcp)

    let getSslStream (client: NetworkStream) (host: string) =
        let sslStream = new SslStream(client)
        sslStream.AuthenticateAsClient(host)
        sslStream

    let ircSendPrivMessage (writer: TextWriter) channel message =
        async { do! writer.WriteLineAsync(PrivMessage channel message) |> Async.AwaitTask }

    let ircPong (writer: TextWriter) message =
        async { do! writer.WriteLineAsync(PongMessage message) |> Async.AwaitTask }

    let ircPartChannel (writer: TextWriter) channel =
        async { do! writer.WriteLineAsync(PartMessage channel) |> Async.AwaitTask }

    let ircJoinChannel (writer: TextWriter) channel =
        async { do! writer.WriteLineAsync(JoinMessage channel) |> Async.AwaitTask }

    let ircJoinChannels (writer: TextWriter) channels =
        let channels = channels |> List.map (fun c -> $"#{c}") |> String.concat ","

        async { do! writer.WriteLineAsync($"JOIN {channels}") |> Async.AwaitTask }
