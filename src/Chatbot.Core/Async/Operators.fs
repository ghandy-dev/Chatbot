[<AutoOpen>]
module Operators

let (|+>) asyncArg func =
    async {
        let! arg = asyncArg
        return func arg
    }
