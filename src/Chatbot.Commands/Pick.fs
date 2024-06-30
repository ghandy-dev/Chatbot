namespace Chatbot.Commands

[<AutoOpen>]
module Pick =

    open System

    let private random = Random.Shared

    let pick args =
        match args with
        | [] -> Error "No items provided."
        | _ ->
            let index = random.Next args.Length
            Ok <| Message $"{args[index]}"
