module List

let doesNotContain value list = not <| (list |> List.contains value)

let tryRandomChoice (source: 'T list) : 'T option = source |> Seq.tryRandomChoice
