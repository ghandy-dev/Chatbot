namespace Chatbot.Commands

[<AutoOpen>]
module DallE =

    open System
    open Chatbot.Commands.Api.DallE
    open Utils.Parsing

    let private toSize =
        function
        | "square" -> "1024x1024"
        | "portrait" -> "1792x1024"
        | "landscape" -> "1024x1792"
        | _ -> "1024x1024"

    let private generateImage size prompt = async { return! getImage size prompt }

    let dalle (args: string list) =
        async {
            match args with
            | [] -> return Error $"Usage: >dalle <your prompt here>"
            | _ ->
                let values =
                    parseKeyValuePairs (String.Join("", args))
                    |> Map.change
                        "size"
                        (fun k ->
                            match k with
                            | Some v -> Some(toSize v)
                            | None -> Some(toSize "")
                        )

                return! generateImage values["size"] values["prompt"]
        }
