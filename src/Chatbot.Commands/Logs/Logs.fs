namespace Commands.Logs

[<AutoOpen>]
module Logs =

    open Api
    open Commands

    let randomLine args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | [] ->
                    match! getChannelRandomLine channel.Channel with
                    | Error err -> return Message err
                    | Ok message -> return Message message
                | [ user ]
                | user :: _ ->
                    match! getUserRandomLine channel.Channel user  with
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
                    match! getUserRandomLine channel.Channel context.Username with
                    | Error err -> return Message err
                    | Ok message -> return Message message
        }
