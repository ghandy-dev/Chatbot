[<RequireQualifiedAccess>]
module Async

let Create v = async { return v }

let Bind binder computation = async.Bind (computation, binder)

let Map f a = Bind (f >> Create) a
