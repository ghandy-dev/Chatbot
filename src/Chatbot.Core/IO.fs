namespace Chatbot.Core

[<RequireQualifiedAccess>]
module IO =

    open System
    open System.IO
    open System.Threading

    let sharedBuffer<'a> = Buffers.ArrayPool<'a>.Shared

    let createStreamWriter (stream: Stream) = new StreamWriter(stream, new Text.UTF8Encoding(false))

    let createStreamReader (stream: Stream) = new StreamReader(stream)

    let writeAsync (writer: StreamWriter) (message: ReadOnlyMemory<char>) (cancellationToken: CancellationToken) = writer.WriteAsync (message, cancellationToken) |> Async.AwaitTask

    let flushAsync (writer: StreamWriter) (cancellationToken: CancellationToken) = writer.FlushAsync(cancellationToken) |> Async.AwaitTask

    let writeLineAsync (writer: StreamWriter) (message: ReadOnlyMemory<char>) (cancellationToken: CancellationToken) = writer.WriteLineAsync(message, cancellationToken) |> Async.AwaitTask

    let readAsync (reader: StreamReader) (buffer: char array) (cancellationToken: CancellationToken) = reader.ReadAsync(buffer, cancellationToken).AsTask() |> Async.AwaitTask

    let readLineAsync (reader: StreamReader) (cancellationToken: CancellationToken) = reader.ReadLineAsync(cancellationToken).AsTask() |> Async.AwaitTask
