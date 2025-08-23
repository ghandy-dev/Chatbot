[<AutoOpen>]
module Utils

open System
open System.Text
open System.Text.RegularExpressions

let [<Literal>] DateStringFormat = "dd/MM/yyyy"
let [<Literal>] TimeStringFormat = "HH:mm:ss"
let [<Literal>] DateTimeStringFormat = $"dd/MM/yyyy HH:mm:ss"
let [<Literal>] UtcDateTimeStringFormat = $"yyyy-MM-ddTHH:mm:ss.ffffZ"

let utcNow () = DateTime.UtcNow
let now () = DateTime.Now
let today () = DateOnly.FromDateTime(utcNow())
let base64 (s: string) = s |> System.Text.Encoding.UTF8.GetBytes |> System.Convert.ToBase64String

let formatTimeSpan (ts: TimeSpan) =
    let formatComponent value =
        if value > 0 then Some (value.ToString()) else None

    let years = if ts.Days >= 365 then Some ((ts.Days / 365).ToString()) else None
    let days = if years.IsSome then formatComponent (ts.Days % 365) else formatComponent ts.Days
    let hours = formatComponent ts.Hours
    let minutes = formatComponent ts.Minutes
    let seconds = formatComponent ts.Seconds

    match years, days, hours, minutes, seconds with
    | Some y, Some d,Some h, _, _ -> sprintf "%sy, %sd, %sh" y d h
    | Some y, None, Some h, _, _ -> sprintf "%sy, %sh" y h
    | Some y, Some d, None, _, _ -> sprintf "%sy, %sd" y d
    | Some y, None , None, _, _ -> sprintf "%sy" y
    | None, Some d, Some h, Some m, _ -> sprintf "%sd, %sh, %sm" d h m
    | None, Some d, None, Some m, _ -> sprintf "%sd, %sm" d m
    | None, Some d, Some h, None, _ -> sprintf "%sd, %sh" d h
    | None, Some d, None, None, _ -> sprintf "%sd" d
    | None, None, Some h, Some m, Some _ -> sprintf "%sh, %sm" h m
    | None, None, Some h, None, Some _ -> sprintf "%sh" h
    | None, None, Some h, Some m, None -> sprintf "%sh, %sm" h m
    | None, None, Some h, None, None -> sprintf "%sh" h
    | None, None, None, Some m, Some s -> sprintf "%sm, %ss" m s
    | None, None, None, Some m, None -> sprintf "%sm" m
    | None, None, None, None, Some s -> sprintf "%ss" s
    | _ -> "0s"


let strFormat (s: string) (args: string list) =
    let pattern = @"\{(\d+)\}"
    Regex.Replace(s, pattern, fun m ->
        let index = int m.Groups.[1].Value
        args.[index])

let strCompare a b = String.Compare(a, b) = 0
let strCompareIgnoreCase a b = String.Compare(a, b, ignoreCase = true) = 0
let strEmpty = String.IsNullOrWhiteSpace
let strNotEmpty = not << strEmpty
let strConcat (values: string seq) = String.Concat(values)
let strJoin (separator: string) (values: string seq) = String.Join(separator, values)

let htmlDecode = System.Web.HttpUtility.HtmlDecode