module Parsing

open FSharpPlus

let tryParseInt : _ -> System.Int32 option = tryParse
let tryParseDateTime : _ -> System.DateTime option = tryParse
let tryParseDateOnly : _ -> System.DateOnly option = tryParse
let tryParseBoolean : _ -> System.Boolean option = tryParse

let parseBit : _ -> System.Boolean =
    function
    | "0" -> false
    | _ -> true
