module Text

open System.Text.RegularExpressions

let formatString (format: string) (args: string list) =
    let pattern = @"\{(\d+)\}"
    Regex.Replace(format, pattern, fun m ->
        let index = int m.Groups.[1].Value
        args.[index])

let stripMarkdownTags content =
    let patterns = [
        @"`{3}", ""                             // Code Blocks
        @"`{1}([\S].*?)`{1}", "$1"              // Inline code
        @"\*{1,2}([\S].*?)\*{1,2}", "$1"        // Bold
        @"-{2,3}", "-"                          // Em/en dash
        @"_{2}([\S].*?)_{2}", "$1"              // Italics
        @"~{2}([\S].*?)~{2}", "$1"              // Strikethrough
        @"#{1,6}\s(.*?)", "$1"                  // Headers
        @"=|-{5,}.*\n", ""                      // Other Headers
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

