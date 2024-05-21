namespace Chatbot

module Program =

    open System
    open System.Threading

    let logger = Logging.createNamedLogger "Program"
    let cancellationTokenSource = new CancellationTokenSource()
    let cancellationToken = cancellationTokenSource.Token

    let cancelSubscription =
        Console.CancelKeyPress.Subscribe(fun args ->
            logger.LogInfo "Cancellation Requested..."
            args.Cancel <- true
            cancellationTokenSource.Cancel()
        )

    [<EntryPoint>]
    let main _ =
        try
            try
                logger.LogInfo "Starting bot..."
                Bot.run cancellationToken
                Async.AwaitWaitHandle cancellationToken.WaitHandle |> ignore
                logger.LogInfo "Bot stopped."
            with ex ->
                logger.LogError("Exception caught.", ex)
        finally
            cancellationTokenSource.Token.WaitHandle.WaitOne() |> ignore

        0
