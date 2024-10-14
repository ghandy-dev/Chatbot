namespace Database

[<RequireQualifiedAccess>]
type DatabaseResult<'TSuccess> =
    | Success of 'TSuccess
    | Failure
