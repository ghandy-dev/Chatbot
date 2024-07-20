[<RequireQualifiedAccess>]
module Async

let create v = async { return v }

let bind binder computation = async.Bind (computation, binder)

let map f a = bind (f >> create) a

