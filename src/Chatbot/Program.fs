module Chatbot.Program

open System
open System.Threading
open Microsoft.Extensions.Configuration

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
    match args with
    | [|"migrate"|] ->
        Logging.info "Running migrations..."

        let configuration =
            ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build()

        let dbConnectionString = configuration.GetValue<string>("ConnectionStrings:Database")

        Chatbot.Database.Migration.run dbConnectionString
    | _ ->
        async {
            try
                Logging.info "Starting..."
                do! Bot.run cancellationToken
                Async.AwaitWaitHandle cancellationToken.WaitHandle |> ignore
            with ex ->
                Logging.errorEx "Exception caught" ex

            cancellationToken.WaitHandle.WaitOne() |> ignore

            Logging.info "Stopped."
        }
        |> Async.RunSynchronously

    0
