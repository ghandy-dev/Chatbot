module List

let doesNotContain value list = not <| List.contains value list

let tryRandomChoice (source: 'T list) : 'T option = source |> Seq.tryRandomChoice
