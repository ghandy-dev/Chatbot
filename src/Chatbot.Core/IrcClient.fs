namespace Chatbot

open Chatbot.IRC

open System

[<AllowNullLiteral>]
type IrcClient(host: string, port: int) =

    let [<Literal>] writerBufferSize = 1024
    let [<Literal>] readerufferSize = 10240

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

    let read cancellationToken =
        IO.readAsync reader readerufferSize cancellationToken

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

    member _.Connected = connected ()

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

    member _.WriteAsync (message: string) = writeLine message

    member _.SendAsync (message: string) = send message

    member _.FlushAsync () = flush ()

    member this.AuthenticateAsync (user: string, accessToken: string, capabilities: string array) =
        async {
            if (capabilities.Length > 0) then
                do! this.WriteAsync($"""CAP REQ :{String.concat " " capabilities}""")

            do! this.WriteAsync($"PASS oauth:{accessToken}")
            do! this.WriteAsync($"NICK {user}")
            do! this.FlushAsync()
        }

    interface IDisposable with
        member _.Dispose () =
            isConnected <- false
            writer.Dispose()
            client.Dispose()
