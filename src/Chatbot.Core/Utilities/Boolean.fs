module Boolean

open System

let tryParse (s: ReadOnlySpan<char>) =
    match System.Boolean.TryParse s with
    | false, _ -> None
    | true, v -> Some v

let tryParseBit =
    function
    | "1" -> Some true
    | "0"
    | "-1" -> Some false
    | _ -> None

let parseBit =
    function
    | "0" -> false
    | _ -> true
