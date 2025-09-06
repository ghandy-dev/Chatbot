namespace Commands

[<AutoOpen>]
module Echo =

    let echo context = Ok <| Message $"""{String.concat " " context.Args}"""
