namespace Chatbot.Common

[<AutoOpen>]
module Utils =

    open System
    open System.Globalization
    open System.Text
    open System.Text.RegularExpressions

    let [<Literal>] DateStringFormat = "dd/MM/yyyy"
    let [<Literal>] TimeStringFormat = "HH:mm:ss"
    let [<Literal>] DateTimeStringFormat = $"dd/MM/yyyy HH:mm:ss"
    let [<Literal>] UtcDateTimeStringFormat = $"yyyy-MM-ddTHH:mm:ss.ffffZ"

    let utcNow () = DateTimeOffset.UtcNow
    let now () = DateTimeOffset.Now
    let epochTime () = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    let epochTimeSeconds () = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    let today () = DateOnly.FromDateTime(utcNow().Date)
    let base64: string -> string = System.Text.Encoding.UTF8.GetBytes >> System.Convert.ToBase64String

    let formatElapsed (start: DateTimeOffset) (``end``: DateTimeOffset) =
        let years = ``end``.Year - start.Year
        let ts = ``end`` - start.AddYears(years)

        let days = ts.Days
        let hours = ts.Hours
        let mins = ts.Minutes
        let secs = ts.Seconds

        let format =
            function
            | y, _, _, _, _ when y > 0 -> [ $"{years}y" ; $"{days}d"; $"{hours}h"; $"{mins}m" ]
            | _, d, _, _, _ when d > 0 -> [ $"{days}d"; $"{hours}h"; $"{mins}m" ]
            | _, _, h, _, _ when h > 0 -> [ $"{hours}h"; $"{mins}m"; $"{secs}s" ]
            | _, _, _, m, _ when m > 0 -> [ $"{mins}m"; $"{secs}s" ]
            | _, _, _, _, s -> [ $"{s}s" ]

        let parts = format (years, days, hours, mins, secs)

        String.concat ", " parts

    let stripMarkdownTags content =
        let patterns = [
            @"`{3}", ""                             // Code Blocks
            @"`{1}([\S].*?)`{1}", "$1"              // Inline code
            @"\*{1,2}([\S].*?)\*{1,2}", "$1"        // Bold
            @"-{2,3}", "-"                          // Em/en dash
            @"_{2}([\S].*?)_{2}", "$1"              // Italics
            @"~{2}([\S].*?)~{2}", "$1"              // Strikethrough
            @"^(?:#{1,6}\s(.*?))", "$1"             // Headers
            @"^(?:={5,}|-{5,})\s*\n", ""            // Other Headers
            @"\[.*?\][\(](.*?)[\)]", "$1"           // Links
            @"\r\n{1,}", " "                        // CRLF
            @"\n{1,}", " "                          // LF
        ]

        let stripped =
            patterns
            |> List.fold (fun acc (pattern, replacement) ->
                Regex.Replace(acc, pattern, replacement, RegexOptions.Multiline)
            ) content

        stripped

    let htmlEncode = System.Web.HttpUtility.HtmlEncode
    let htmlDecode = System.Web.HttpUtility.HtmlDecode

    let private isDangerousRune (rune: Rune) =
        let v = rune.Value
        let cat = Rune.GetUnicodeCategory rune

        let allowedFormatChars = v = 0x200D // Zero Width Joiner (used by some emojis)

        if allowedFormatChars then
            false
        else
            match cat with
            | UnicodeCategory.Control
            | UnicodeCategory.PrivateUse
            | UnicodeCategory.Surrogate
            | UnicodeCategory.Format
            | UnicodeCategory.OtherNotAssigned  ->
                true

            | _ ->
                let isTagChar =
                    v >= 0xE0000 && v <= 0xE007F

                let isNonCharacter =
                    v >= 0xFDD0 && v <= 0xFDEF ||
                    v &&& 0xFFFE = 0xFFFE ||
                    v = 0x034F // Combining grapheme joiner

                // https://www.unicode.org/versions/Unicode17.0.0/core-spec/chapter-5/#G40025
                // Default Ignorable Code Point
                let defaultIgnorableCodePointChar =
                    v >= 0x2060 && v <= 0x206F ||
                    v >= 0xFFF0 && v <= 0xFFF8 ||
                    v >= 0xE0000 && v <= 0xE0FFF

                isTagChar || isNonCharacter || defaultIgnorableCodePointChar

    let cleanInput (input: string) =
        let normalized = input.Normalize(NormalizationForm.FormKC)
        let sb = StringBuilder()

        for rune in normalized.EnumerateRunes() do
            if not (isDangerousRune rune) then
                sb.Append(rune.ToString()) |> ignore

        sb.ToString()
