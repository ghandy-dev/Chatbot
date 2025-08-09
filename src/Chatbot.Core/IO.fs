[<RequireQualifiedAccess>]
module IO

open System
open System.IO
open System.Threading

let private sharedBuffer<'a> = Buffers.ArrayPool<'a>.Shared

let createStreamWriter (stream: Stream) bufferSize =
    new StreamWriter(stream, new Text.UTF8Encoding(false), bufferSize) |> TextWriter.Synchronized

let createStreamReader (stream: Stream) =
    new StreamReader(stream) |> TextReader.Synchronized

let writeAsync (writer: TextWriter) (message: ReadOnlyMemory<char>) (cancellationToken: CancellationToken) = writer.WriteAsync (message, cancellationToken) |> Async.AwaitTask

let flushAsync (writer: TextWriter) (cancellationToken: CancellationToken) = writer.FlushAsync(cancellationToken) |> Async.AwaitTask

let writeLineAsync (writer: TextWriter) (message: ReadOnlyMemory<char>) (cancellationToken: CancellationToken) = writer.WriteLineAsync(message, cancellationToken) |> Async.AwaitTask

let readAsync (reader: TextReader) (bufferSize: int) (cancellationToken: CancellationToken) =
    async {
        let buffer = sharedBuffer<char>.Rent(bufferSize)
        let memory = new Memory<char>(buffer)

        try
            let! bytesRead = reader.ReadAsync(memory, cancellationToken).AsTask() |> Async.AwaitTask

            if bytesRead > 0 then
                let message = memory.Slice(0, bytesRead).ToString()
                return Some message
            else
                return None
        finally
            sharedBuffer.Return(buffer)
    }
