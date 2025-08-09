module Json

open System.Text.Json

let serializeJson (value: 'T) = JsonSerializer.Serialize<'T> (value, JsonSerializerOptions.Web)
let deserializeJson<'T> (json: string) = JsonSerializer.Deserialize<'T> (json, JsonSerializerOptions.Web)