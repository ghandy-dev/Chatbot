[<AutoOpen>]
module Operators

let (|+->) asyncArg func =
    async {
        let! arg = asyncArg
        return func arg
    }

let (|-+>) arg asyncFunc =
    async {
        return! asyncFunc arg
    }

let (|++>) asyncArg asyncFunc =
    async {
        let! arg = asyncArg
        return! asyncFunc arg
    }
