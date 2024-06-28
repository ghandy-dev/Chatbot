namespace Chatbot

module Program =

    open System
    open System.Threading

    let logger = Logging.createNamedLogger "Program" (Some Logging.LogLevel.Trace)
    let cancellationTokenSource = new CancellationTokenSource()
    let cancellationToken = cancellationTokenSource.Token

    let cancelSubscription =
        Console.CancelKeyPress.Subscribe(fun args ->
            logger.LogInfo "Cancellation Requested..."
            args.Cancel <- true
            cancellationTokenSource.Cancel()
        )

    [<EntryPoint>]
    let main args =
        try
            try
                logger.LogInfo "Starting..."
                Bot.run cancellationToken
                Async.AwaitWaitHandle cancellationToken.WaitHandle |> ignore
            with ex ->
                logger.LogError("Exception caught.", ex)
        finally
            cancellationTokenSource.Token.WaitHandle.WaitOne() |> ignore

        logger.LogInfo "Stopped."

        0
