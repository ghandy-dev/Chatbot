namespace Chatbot.Common

module UrlBuilder =

    open System

    let buildUrl url (parameters: (string * string) seq) =
        let query =
            parameters
            |> Seq.map (fun (k, v) -> $"{k}={Uri.EscapeDataString(v)}")
            |> strJoin "&"

        $"{url}?{query}"
