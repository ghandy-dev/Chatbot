namespace Commands.OpenAI

[<AutoOpen>]
module OpenAI =

    open Commands
    open Api

    let private toSize =
        function
        | "square" -> "1024x1024"
        | "portrait" -> "1792x1024"
        | "landscape" -> "1024x1792"
        | _ -> "1024x1024"

    let private generateImage size prompt = async { return! getImage size prompt }

    let private dalleKeys = [ "size" ]

    let dalle args =
        async {
            match args with
            | [] -> return Message $"No prompt provided"
            | _ ->
                let keyValues = KeyValueParser.parse args dalleKeys
                let prompt = KeyValueParser.removeKeyValues args dalleKeys |> String.concat " "

                let size = keyValues.TryFind "size" |?? "square"

                match! generateImage size prompt with
                | Error err -> return Message err
                | Ok url -> return Message url
        }

    let private gptKeys = [ "persona" ]

    let gpt args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "Gpt currently cannot be used in whispers"
            | Channel channel ->
                match args with
                | [] -> return Message "No input provided"
                | input ->
                    let keyValues = KeyValueParser.parse input gptKeys
                    let gptKey = keyValues |> Map.tryFind "persona" |?? "default"

                    let message = KeyValueParser.removeKeyValues args gptKeys |> String.concat " "

                    match! sendGptMessage message context.Username channel.Channel gptKey with
                    | Error err -> return Message err
                    | Ok message -> return Message(Text.stripMarkdownTags message)
        }
