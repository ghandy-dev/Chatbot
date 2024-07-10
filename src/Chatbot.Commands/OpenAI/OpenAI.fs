namespace Chatbot.Commands.OpenAI

[<AutoOpen>]
module OpenAI =

    open System

    open Chatbot.Commands
    open Api
    open Utils

    let private toSize =
        function
        | "square" -> "1024x1024"
        | "portrait" -> "1792x1024"
        | "landscape" -> "1024x1792"
        | _ -> "1024x1024"

    let private generateImage size prompt = async { return! getImage size prompt }

    let dalle args =
        async {
            match args with
            | [] -> return Error $"No prompt provided"
            | _ ->
                let values =
                    Text.parseKeyValuePairs (String.Join("", args))
                    |> Map.change
                        "size"
                        (fun k ->
                            match k with
                            | Some v -> Some(toSize v)
                            | None -> Some(toSize "")
                        )

                match! generateImage values["size"] values["prompt"] with
                | Error err -> return Error err
                | Ok url -> return Ok <| Message url
        }

    let gpt args context =
        async {
            match context.Source with
            | Whisper _ -> return Ok <| Message "Gpt currently cannot be used in whispers"
            | Channel channel ->
                match args with
                | [] -> return Error "No input provided"
                | input ->
                    let message = String.concat " " input

                    match! sendGptMessage message context.Username channel with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message(Text.stripMarkdownTags message)
        }
