module String

open System
open System.Text.RegularExpressions

let format (s: string) (args: string list) =
    let pattern = @"\{(\d+)\}"
    Regex.Replace(s, pattern, fun m ->
        let index = int m.Groups.[1].Value
        args.[index])

let compare a b = String.Compare(a, b) = 0
let compareIgnoreCase a b = String.Compare(a, b, ignoreCase = true) = 0
let isEmpty = String.IsNullOrWhiteSpace
let join (separator: string) (values: string seq) = String.Join(separator, values)
let startsWith (value: string) (s: string) = s.StartsWith(value)
let replace (oldValue: string) (newValue: string) (s: string) = s.Replace(oldValue, newValue)
let split (separator: string) (s: string) = s.Split(separator, StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
let subString length (s: string) = s.Substring(length)
