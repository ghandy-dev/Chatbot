namespace Database

[<RequireQualifiedAccess>]
type DatabaseResult<'TSuccess> =
    | Success of 'TSuccess
    | Failure

module DatabaseResult =

    let toResult dr =
        match dr with
        | DatabaseResult.Failure -> Error ()
        | DatabaseResult.Success s -> Ok s