module Map

let private add = fun acc key value -> Map.add key value acc

let merge (a: Map<'a, 'b>) (b: Map<'a, 'b>) =

    if a.Count < b.Count then
        Map.fold add b a
    else
        Map.fold add a b

let mergeInto (into: Map<'a, 'b>) (from: Map<'a, 'b>) = Map.fold add into from

let mergeFrom = fun into from -> mergeInto from into