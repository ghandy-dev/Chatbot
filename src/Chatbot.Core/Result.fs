[<RequireQualifiedAccess>]
module Result

let bindAsync binder asyncValue =
    async {
        match! asyncValue with
        | Ok value -> return! binder value
        | Error error -> return Error error
    }

let bindTAsync binder asyncValue =
    async {
        match! asyncValue with
        | Ok value -> return! (binder value) |> Async.AwaitTask
        | Error error -> return Error error
    }

let zipAsync a b =
    async {
        match! a with
        | Ok x ->
            match! b with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let bindZipAsync binder asyncValue =
    async {
        match! asyncValue with
        | Ok x ->
            match! binder x with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let bindZip binder value =
    match value with
    | Ok x ->
        match binder x with
        | Ok y -> Ok(x, y)
        | Error error -> Error error
    | Error error -> Error error

let fromOption error option =
    match option with
    | None -> Error error
    | Some value -> Ok value

let toOptionAsync asyncResult =
    async {
        let! result = asyncResult
        return Result.toOption result
    }

let fromOptionAsync error asyncOption =
    async {
        let! option = asyncOption
        return fromOption error option
    }
