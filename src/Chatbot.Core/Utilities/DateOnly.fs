module DateOnly

open System

let today() = DateOnly.FromDateTime(utcNow())

let tryParseExact (s: ReadOnlySpan<char>) (format: ReadOnlySpan<char>) =
    match DateOnly.TryParseExact(s, format) with
    | false, _ -> None
    | true, date -> Some date