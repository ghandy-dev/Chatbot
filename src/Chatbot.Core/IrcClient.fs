namespace Chatbot

open Chatbot.IRC

open System
open System.Net.Sockets

type IrcClient(host: string, port: int) =

    let [<Literal>] writerBufferSize = 1024
    let [<Literal>] readerBufferSize = 10240

    let socket = IRC.createTcpClient host port
    let mutable reader = null
    let mutable writer = null

    let mutable isConnected = true

    let connected () =
        if isConnected = false then
            false
        else
            socket.Connected

    let connect cancellationToken =
        async {
            do! socket.ConnectAsync(host, port, cancellationToken).AsTask() |> Async.AwaitTask
            let stream = new NetworkStream(socket)
            let secureStream = IRC.getSslStream stream host
            reader <- IO.createStreamReader secureStream
            writer <- IO.createStreamWriter secureStream writerBufferSize
        }

    let read cancellationToken =
        IO.readAsync reader readerBufferSize cancellationToken

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

    interface ITwitchConnection with
        member _.Connected = connected ()

        member _.ConnectAsync (cancellationToken) = connect cancellationToken

        member _.PongAsync (message) =
            async {
                do! IRC.ircPong writer message
                do! flush ()
            }

        member _.PartChannelAsync (channel) =
            async {
                do! IRC.ircPartChannel writer channel
                do! flush ()
            }

        member _.JoinChannelAsync (channel) =
            async {
                do! IRC.ircJoinChannel writer channel
                do! flush ()
            }

        member _.JoinChannelsAsync (channels) =
            async {
                do! IRC.ircJoinChannels writer channels
                do! flush ()
            }

        member _.SendPrivMessageAsync (channel, message) =
            async {
                do! IRC.ircSendPrivMessage writer channel message
                do! flush ()
            }

        member _.ReadAsync (cancellationToken) = read cancellationToken

        member _.SendAsync (message: string) = send message


        member _.AuthenticateAsync (user: string, accessToken: string, capabilities: string array) =
            async {
                if (capabilities.Length > 0) then
                    do! writeLine($"""CAP REQ :{String.concat " " capabilities}""")

                do! writeLine($"PASS oauth:{accessToken}")
                do! writeLine($"NICK {user}")
                do! flush()
            }

    interface IDisposable with
        member _.Dispose () =
            isConnected <- false
            writer.Dispose()
            socket.Dispose()
