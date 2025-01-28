namespace Commands

[<AutoOpen>]
module Logs =

    open Commands

    let randomLine args context =
        async {
            match context.Source with
            | Whisper _ -> return Message "This command is only avaiable in channels"
            | Channel channel ->
                match args with
                | [] ->
                    match! IVR.getChannelRandomLine channel.Channel with
                    | Error err -> return Message err
                    | Ok message -> return Message message
                | [ user ]
                | user :: _ ->
                    match! IVR.getUserRandomLine channel.Channel user  with
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
                    match! IVR.getUserRandomLine channel.Channel context.Username with
                    | Error err -> return Message err
                    | Ok message -> return Message message
        }
