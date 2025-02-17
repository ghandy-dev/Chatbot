module Int32

open System

let tryParse (s: ReadOnlySpan<char>) =
    match System.Int32.TryParse(s) with
    | true, v -> Some v
    | false, _ -> None

let positive = fun n -> n >= 0 // 🤓 0 isn't positive
let negative = fun n -> n < 0