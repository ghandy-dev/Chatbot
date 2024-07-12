[<RequireQualifiedAccess>]
module AsyncResult

let map mapping asyncValue =
    async {
        match! asyncValue with
        | Ok value -> return Ok(mapping value)
        | Error error -> return Error error
    }

let bind binder asyncValue =
    async {
        match! asyncValue with
        | Ok value -> return! binder value
        | Error error -> return Error error
    }

let bindT binder asyncValue =
    async {
        match! asyncValue with
        | Ok value -> return! (binder value) |> Async.AwaitTask
        | Error error -> return Error error
    }

let bindAsyncSync binder value =
    async {
        match value with
        | Ok value -> return! binder value
        | Error error -> return Error error
    }

let bindSyncAsync binder asyncValue =
    async {
        match! asyncValue with
        | Ok value -> return binder value
        | Error error -> return Error error
    }

let zip a b =
    async {
        match! a with
        | Ok x ->
            match! b with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let zipSyncAsync a (b: Async<Result<'c, 'b>>) =
    async {
        match a with
        | Ok x ->
            match! b with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let zipAsyncSync a b =
    async {
        match! a with
        | Ok x ->
            match b with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let bindZip binder asyncValue =
    async {
        match! asyncValue with
        | Ok x ->
            match! binder x with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let bindZipResult binder value =
    async {
        match value with
        | Ok x ->
            match! binder x with
            | Ok y -> return Ok(x, y)
            | Error error -> return Error error
        | Error error -> return Error error
    }

let toOption asyncResult =
    async {
        let! result = asyncResult
        return Result.toOption result
    }

let fromOption error asyncOption =
    async {
        let! option = asyncOption
        return Result.fromOption error option
    }
