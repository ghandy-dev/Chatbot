module Pages.Command

open Chatbot.Commands
open Html

let private generate' (ctx: SiteContents) (page: string) =
    let commands = ctx.TryGetValue<Types.CommandPage seq>() |> Option.defaultValue Seq.empty

    let command = commands |> Seq.find (fun c -> c.Command = page)

    let content =
        div [] [
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
                    td [] [ yield! command.ExampleUsage.Split("\n") |> Array.map (fun l ->  p [] [ !! (System.Web.HttpUtility.HtmlEncode l) ] ) ]
                ]
            ]
        ]

    content |> Layout.render ctx


let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
