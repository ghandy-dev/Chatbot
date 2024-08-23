namespace Chatbot.Commands.Logs

[<AutoOpen>]
module Logs =

    open Api
    open Chatbot.Commands

    let randomLine args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | [] ->
                    match! getChannelRandomLine channel with
                    | Error err -> return Message err
                    | Ok message -> return Message message
                | [ user ]
                | user :: _ ->
                    match! getUserRandomLine channel user  with
                    | Error err -> return Message err
                    | Ok message -> return Message message
        }

    let randomQuote args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | _ ->
                    match! getUserRandomLine channel context.Username with
                    | Error err -> return Message err
                    | Ok message -> return Message message
        }
