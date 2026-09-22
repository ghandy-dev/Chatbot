module Chatbot.Connection

open System
open System.Net.Security
open System.Net.Sockets

open FsToolkit.ErrorHandling

open Chatbot.Common

type Connection(host: string, port: int) =

    let client = new TcpClient()
    let mutable reader = null
    let mutable writer = null

    member _.ConnectAsync cancellationToken =
        async {
            let! result =
                client.ConnectAsync(host, port, cancellationToken).AsTask() |> Async.AwaitTask
                |> Async.Catch
                |> Async.map Result.ofChoice


            match result with
            | Ok _ ->
                let sslStream = new SslStream(client.GetStream())
                do! sslStream.AuthenticateAsClientAsync(host) |> Async.AwaitTask
                reader <- IO.createStreamReader sslStream
                writer <- IO.createStreamWriter sslStream
            | _ -> ()

            return result
        }

    member _.ReadAsync cancellationToken =
        async {
            return!
                IO.readLineAsync reader cancellationToken
                |> Async.Catch
                |> Async.map Result.ofChoice
        }

    member _.SendAsync (message: string, cancellationToken) =
        async {
            do! IO.writeLineAsync writer (message.AsMemory()) cancellationToken
            do! IO.flushAsync writer cancellationToken
        }
        |> Async.Catch
        |> Async.map Result.ofChoice

    interface IDisposable with

        member _.Dispose () =
            if reader <> null then
                reader.Dispose()

            if writer <> null then
                writer.Dispose()

            client.Dispose()