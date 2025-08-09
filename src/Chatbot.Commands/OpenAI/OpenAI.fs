namespace Commands

[<AutoOpen>]
module OpenAI =

    open System.Text.RegularExpressions

    open FsToolkit.ErrorHandling

    open CommandError
    open OpenAI.Api

    let private toSize =
        function
        | "square" -> "1024x1024"
        | "portrait" -> "1792x1024"
        | "landscape" -> "1024x1792"
        | _ -> "1024x1024"

    let private dalleKeys = [ "size" ]

    let dalle args =
        asyncResult {
            match args with
            | [] -> return! invalidArgs $"No prompt provided"
            | _ ->
                let kvp = KeyValueParser.parse args dalleKeys
                let prompt = kvp.Input |> String.concat " "
                let size = kvp.KeyValues.TryFind "size" |? "square"

                let! url = getImage size prompt |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")
                return Message url
        }

    let private gptKeys = [ "persona" ]

    let private stripMarkdownTags content =
        let patterns = [
            @"`{3}", ""                             // Code Blocks
            @"`{1}([\S].*?)`{1}", "$1"              // Inline code
            @"\*{1,2}([\S].*?)\*{1,2}", "$1"        // Bold
            @"-{2,3}", "-"                          // Em/en dash
            @"_{2}([\S].*?)_{2}", "$1"              // Italics
            @"~{2}([\S].*?)~{2}", "$1"              // Strikethrough
            @"#{1,6}\s(.*?)", "$1"                  // Headers
            @"=|-{5,}.*\n", ""                      // Other Headers
            @"\[.*?\][\(](.*?)[\)]", "$1"           // Links
            @"\r\n{1,}", " "                        // CRLF
            @"\n{1,}", " "                          // LF
        ]

        let stripped =
            patterns
            |> List.fold (fun acc (pattern, replacement) ->
                Regex.Replace(acc, pattern, replacement, RegexOptions.Multiline)
            ) content

        stripped

    let gpt args context =
        asyncResult {
            match context.Source with
            | Whisper _ -> return! invalidArgs "Gpt currently cannot be used in whispers"
            | Channel channel ->
                match args with
                | [] -> return! invalidArgs "No input provided"
                | input ->
                    let kvp = KeyValueParser.parse input gptKeys
                    let gptKey = kvp.KeyValues.TryFind "persona" |? "default"

                    let message = kvp.Input |> String.concat " "

                    let! response =
                        sendGptMessage message context.Username channel.Channel gptKey
                        |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "OpenAI")
                        |> AsyncResult.map stripMarkdownTags

                    return Message response
        }
