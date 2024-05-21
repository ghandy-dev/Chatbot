[<RequireQualifiedAccess>]
module AsyncOption

let bind binder opt =
    async {
        match! opt with
        | None -> return None
        | Some value -> return! binder value
    }
