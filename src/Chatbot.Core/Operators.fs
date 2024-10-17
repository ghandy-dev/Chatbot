[<AutoOpen>]
module Operators

// Async Piping
let (|+>) a f = Async.bind f a

// Async Piping from non-async
let (|->) a f = Async.bind (f >> Async.create) a

// Null coalescing for System.Nullable<'T>
let (|?) (a: System.Nullable<'a>) b = if a.HasValue then a.Value else b

// Option coalescing (equivalent of Option.defaultValue)
let (|??) (a: 'a option) b =
    match a with
    | None -> b
    | Some v -> v
