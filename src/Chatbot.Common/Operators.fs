[<AutoOpen>]
module Operators

// Option coalescing (equivalent of Option.defaultValue)
let (|?) (a: 'a option) b =
    match a with
    | None -> b
    | Some v -> v
