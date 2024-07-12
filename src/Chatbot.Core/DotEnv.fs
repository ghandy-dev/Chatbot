module DotEnv

open System
open System.IO

let private parseLine (line: string) =
    match line.Split('=', StringSplitOptions.RemoveEmptyEntries) with
    | [| key ; value |] -> Some(key, value)
    | _ -> None

let load () =
    async {
        let filePath = Path.Combine(".env")

        if (filePath |> File.Exists) then
            let! lines = filePath |> File.ReadAllLinesAsync |> Async.AwaitTask
            lines
            |> Seq.choose parseLine
            |> Seq.iter (fun (key, value) -> Environment.SetEnvironmentVariable(key, value))
    }
