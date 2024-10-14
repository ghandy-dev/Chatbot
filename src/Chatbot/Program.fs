module Program

open System
open System.Threading

let cancellationTokenSource = new CancellationTokenSource()
let cancellationToken = cancellationTokenSource.Token

let cancelSubscription =
    Console.CancelKeyPress.Subscribe(fun args ->
        Logging.info "Cancellation Requested..."
        args.Cancel <- true
        cancellationTokenSource.Cancel()
    )

[<EntryPoint>]
let main args =
    async {
        try
            try
                Logging.info "Starting..."
                do! Bot.run cancellationToken
                Async.AwaitWaitHandle cancellationToken.WaitHandle |> ignore
            with ex ->
                Logging.error "Exception caught" ex
        finally
            cancellationTokenSource.Token.WaitHandle.WaitOne() |> ignore

        Logging.info "Stopped."
    } |> Async.RunSynchronously

    0
