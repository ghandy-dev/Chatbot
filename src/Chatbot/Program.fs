module Chatbot.Program

type Program = class end

open System
open System.Threading

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

open CompositionRoot

let cancellationTokenSource = new CancellationTokenSource()
let cancellationToken = cancellationTokenSource.Token
let logger = CompositionRoot.loggerFactory.CreateLogger<Program>()

let cancelSubscription =
    Console.CancelKeyPress.Subscribe(fun args ->
        logger.LogInformation("Cancellation Requested...")
        args.Cancel <- true
        cancellationTokenSource.Cancel()
    )


[<EntryPoint>]
let main args =
    match args with
    | [|"migrate"|] ->
        logger.LogInformation("Running migrations...")

        let dbConnectionString = configuration.GetValue<string>("ConnectionStrings:Database")

        Chatbot.Database.Migration.run dbConnectionString
    | _ ->
        async {
            try
                logger.LogInformation("Starting...")
                do! Bot.run cancellationToken
                Async.AwaitWaitHandle cancellationToken.WaitHandle |> ignore
            with ex ->
                logger.LogCritical(ex, "Exception caught")

            cancellationToken.WaitHandle.WaitOne() |> ignore

            logger.LogInformation("Stopped.")
        }
        |> Async.RunSynchronously

    0
