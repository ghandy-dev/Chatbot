module DotEnv

open System
open System.IO

let private parseLine (line: string) =
    match line.Split('=', StringSplitOptions.RemoveEmptyEntries) with
    | [| key ; value |] -> Environment.SetEnvironmentVariable(key, value)
    | _ -> ()

let load () =
    async {
        let filePath = Path.Combine(".env")

        return!
            filePath
            |> File.Exists
            |> function
                | false -> async { return () }
                | true ->
                    async {
                        let! lines = filePath |> File.ReadAllLinesAsync |> Async.AwaitTask

                        return lines |> Seq.iter parseLine
                    }
    }
