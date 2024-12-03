module Pages.Command

open Commands
open Html

let private generate' (ctx: SiteContents) (page: string) =
    let commands = ctx.TryGetValue<Types.CommandPage seq>() |> Option.defaultValue Seq.empty

    let command = commands |> Seq.find (fun c -> c.Command = page)

    let content =
        div [] [
            div [] [
                a [ Href "" ] [ !! "Commands" ]
            ]

            h1 [] [ !! command.Title ]

            table [] [
                colgroup [] [
                    col []
                    col []
                    // col [ HtmlProperties.Span 1 ; HtmlProperties.Style [ CSSProperties.Width "15%" ] ]
                    // col [ HtmlProperties.Span 1 ; HtmlProperties.Style [ CSSProperties.Width "75%" ] ]
                ]

                tr [] [
                    td [] [ !! "Description" ]
                    td [] [ !! command.Description ]
                ]

                tr [] [
                    td [] [ !! "Command" ]
                    td [] [ !! command.Command ]
                ]

                tr [] [
                    td [] [ !! "Aliases" ]
                    td [] [ !! (command.Aliases |> String.concat ", ") ]
                ]
                tr [] [
                    td [] [ !! "Cooldown" ]
                    td [] [ !! $"{command.Cooldown} seconds" ]
                ]
                tr [] [
                    td [] [ !! "Admin only?" ]
                    td [] [ !! (if command.AdminOnly then "✓" else "✘") ]
                ]
                tr [] [
                    td [] [ !! "Usage" ]
                    td [] [
                        let groupedLines = command.ExampleUsage.Split("\n\n")
                        yield!
                            seq {
                                for i in 0 .. groupedLines.Length - 1 do
                                    let groupedLine = groupedLines[i]
                                    let lines = groupedLine |> _.Split("\n")

                                    yield
                                        div [] [
                                            yield!
                                                seq {
                                                    for j in 0 .. lines.Length - 1 do
                                                        let line = lines[j]

                                                        if line.StartsWith(">") then
                                                            yield code [] [ !! (System.Web.HttpUtility.HtmlEncode line) ]
                                                            yield br []
                                                        else
                                                            yield div [] [ !! (System.Web.HttpUtility.HtmlEncode line) ]
                                                }
                                            ]

                                    yield br []
                            }
                    ]
                ]
            ]
        ]

    content |> Layout.render ctx

let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
