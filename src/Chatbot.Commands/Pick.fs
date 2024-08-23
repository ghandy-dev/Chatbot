namespace Chatbot.Commands

[<AutoOpen>]
module Pick =

    open System
    open System.Text.RegularExpressions

    let private random = Random.Shared

    let pick args =
        match args with
        | [] -> Message "No items provided"
        | head :: tail ->
            let delimiterPattern = @"^delimiter:(.+)$"
            let m = Regex.Match(head, delimiterPattern)

            let items =
                match m.Success with
                | false -> args
                | true ->
                    String.concat " " tail
                    |> _.Split(m.Groups[1].Value, StringSplitOptions.TrimEntries)
                    |> List.ofArray

            let index = random.Next items.Length
            Message $"{items[index]}"
