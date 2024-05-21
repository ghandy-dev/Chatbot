[<RequireQualifiedAccess>]
module Result

let bindAsync asyncBinder value =
    async {
        match value with
        | Ok value -> return! asyncBinder value
        | Error error -> return Error error
    }

let toOption result =
    match result with
    | Ok value -> Some value
    | Error _ -> None

let fromOption error option =
    match option with
    | None -> Error error
    | Some value -> Ok value
