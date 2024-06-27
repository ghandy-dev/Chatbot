namespace Chatbot.Commands

[<AutoOpen>]
module Logs =

    open Chatbot.Commands.Api.Logs

    let randomQuote args context =
        async {
            match context.Source with
            | Whisper _ -> return Ok <| Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | _ ->
                    match! getChannelRandomLine channel with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message message
        }

    let randomLine args context =
        async {
            match context.Source with
            | Whisper _ -> return Ok <| Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | [] ->
                    match! getUserRandomLine channel context.Username with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message message
                | user :: _ ->
                    match! getUserRandomLine channel user with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message message
        }
