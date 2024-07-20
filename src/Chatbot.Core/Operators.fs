[<AutoOpen>]
module Operators

let (|+>) a f = Async.bind f a

let (|->) a f = Async.bind (f >> Async.create) a