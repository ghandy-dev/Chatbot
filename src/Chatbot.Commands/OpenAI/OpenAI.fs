namespace Chatbot.Commands.OpenAI

[<AutoOpen>]
module OpenAI =

    open Chatbot.Commands
    open Api

    let private toSize =
        function
        | "square" -> "1024x1024"
        | "portrait" -> "1792x1024"
        | "landscape" -> "1024x1792"
        | _ -> "1024x1024"

    let private generateImage size prompt = async { return! getImage size prompt }

    let private keys = [ "size" ]

    let dalle args =
        async {
            match args with
            | [] -> return Message $"No prompt provided"
            | _ ->
                let keyValues = KeyValueParser.parse args keys
                let prompt = KeyValueParser.removeKeyValues args keys |> String.concat " "

                let size = keyValues.TryFind "size" |> Option.defaultValue "square"

                match! generateImage size prompt with
                | Error err -> return Message err
                | Ok url -> return Message url
        }

    let gpt args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "Gpt currently cannot be used in whispers"
            | Channel channel ->
                match args with
                | [] -> return Message "No input provided"
                | input ->
                    let message = String.concat " " input

                    match! sendGptMessage message context.Username channel false with
                    | Error err -> return Message err
                    | Ok message -> return Message(Text.stripMarkdownTags message)
        }

    let evilgpt args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "Gpt currently cannot be used in whispers"
            | Channel channel ->
                match args with
                | [] -> return Message "No input provided"
                | input ->
                    let message = String.concat " " input

                    match! sendGptMessage message context.Username channel true with
                    | Error err -> return Message err
                    | Ok message -> return Message(Text.stripMarkdownTags message)
        }
