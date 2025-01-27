module System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization

[<Sealed>]
type UnixEpochDateTimeOffsetConverter() =
    inherit JsonConverter<DateTimeOffset>()

    static let epoch = DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        match reader.TryGetInt64() with
        | false, _ -> raise (JsonException())
        | true, timestamp -> DateTimeOffset.FromUnixTimeMilliseconds(timestamp)

    override _.Write(writer: Utf8JsonWriter, value: DateTimeOffset, _options: JsonSerializerOptions) =
        let unixTime = int64 ((value - epoch).TotalMilliseconds)
        writer.WriteNumberValue(unixTime)

[<Sealed>]
type UnixEpochDateTimeConverter() =
    inherit JsonConverter<DateTime>()

    static let epoch = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        match reader.TryGetInt64() with
        | false, _ -> raise (JsonException())
        | true, timestamp -> epoch.AddMilliseconds(double timestamp).ToLocalTime()

    override _.Write(writer: Utf8JsonWriter, value: DateTime, _options: JsonSerializerOptions) =
        let unixTime = int64 ((value.ToUniversalTime() - epoch).TotalMilliseconds)
        writer.WriteNumberValue(unixTime)
