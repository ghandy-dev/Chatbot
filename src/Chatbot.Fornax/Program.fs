module Program

// #nowarn "20"

open System
open System.IO

open Microsoft.AspNetCore.Builder

[<EntryPoint>]
let main args =
    async {
        match args |> List.ofArray with
        | [] -> failwith "Expected 1 arguments"
        | "build" :: _ -> do! Build.build()
        | "serve" :: _ -> Serve.serve()
        | _ -> failwithf "Unknown switch"


        return 0
    }
    |> Async.RunSynchronously
