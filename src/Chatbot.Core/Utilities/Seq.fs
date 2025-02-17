module Seq

let tryRandomChoice (source: 'T seq) : 'T option =
    if source |> Seq.length = 0 then
        None
    else
        source |> Seq.randomChoice |> Some