namespace System.Text.Json.Serialization

open System
open System.Text.Json
open System.Text.Json.Serialization

[<Sealed>]
type NonCompliantDateTimeOffsetConverter() =
    inherit JsonConverter<DateTimeOffset>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.String ->
            let value = reader.GetString()
            DateTimeOffset.Parse(value)
        | _ -> raise (JsonException())

    override _.Write(writer: Utf8JsonWriter, value: DateTimeOffset, _options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())

[<Sealed>]
type NonCompliantDateTimeConverter() =
    inherit JsonConverter<DateTime>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.String ->
            let value = reader.GetString()
            DateTime.Parse(value)
        | _ -> raise (JsonException())

    override _.Write(writer: Utf8JsonWriter, value: DateTime, _options: JsonSerializerOptions) =
        writer.WriteStringValue(value.ToString())
