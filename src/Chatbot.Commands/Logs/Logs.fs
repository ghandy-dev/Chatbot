namespace Chatbot.Commands

[<AutoOpen>]
module Logs =

    open Chatbot.Commands.Api.Logs

    let randomLine args context =
        async {
            match context.Source with
            | Whisper _ -> return Ok <| Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | [ user ]
                | user :: _ ->
                    match! getUserRandomLine channel user  with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message message
                | [] ->
                    match! getChannelRandomLine channel with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message message
        }

    let randomQuote args context =
        async {
            match context.Source with
            | Whisper _ -> return Ok <| Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | _ ->
                    match! getUserRandomLine channel context.Username with
                    | Error err -> return Error err
                    | Ok message -> return Ok <| Message message
        }
