[<RequireQualifiedAccess>]
module Option

let bindAsync binder opt =
    async {
        match! opt with
        | None -> return None
        | Some value -> return! binder value
    }
