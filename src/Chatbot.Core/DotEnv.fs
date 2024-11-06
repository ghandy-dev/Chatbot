module DotEnv

open System
open System.IO

let private parseLine (line: string) =
    match line.Split('=', StringSplitOptions.RemoveEmptyEntries) with
    | [| key ; value |] -> Some(key, value)
    | _ -> None

let load () =
    async {
        let fileName = ".env"

        if File.Exists(fileName) then
            let! lines = fileName |> File.ReadAllLinesAsync |> Async.AwaitTask

            lines
            |> Seq.choose parseLine
            |> Seq.iter Environment.SetEnvironmentVariable
    }
