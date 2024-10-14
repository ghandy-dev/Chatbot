[<RequireQualifiedAccess>]
module IO

open System
open System.IO

let private sharedBuffer<'a> = Buffers.ArrayPool<'a>.Shared

let createStreamWriter (stream: Stream) bufferSize =
    new StreamWriter(stream, new Text.UTF8Encoding(false), bufferSize) |> TextWriter.Synchronized

let createStreamReader (stream: Stream) =
    new StreamReader(stream) |> TextReader.Synchronized

let writeAsync (writer: TextWriter) (message: string) =
    async { do! writer.WriteAsync(message) |> Async.AwaitTask }

let flushAsync (writer: TextWriter) =
    async { do! writer.FlushAsync() |> Async.AwaitTask }

let writeLineAsync (writer: TextWriter) (message: string) =
    async { do! writer.WriteLineAsync(message) |> Async.AwaitTask }

let readAsync (reader: TextReader) bufferSize cancellationToken =
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
