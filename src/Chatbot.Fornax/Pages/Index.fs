module Pages.Index

open Chatbot.Commands
open Html

let private generate' (ctx: SiteContents) (page: string) =
    let commands =
        table [] [
            colgroup [ Class "table-heading" ] [
                col [ HtmlProperties.Span 2 ; HtmlProperties.Style [ CSSProperties.Width "5%" ] ]
                col [ HtmlProperties.Span 2 ; HtmlProperties.Style [ CSSProperties.Width "5%" ] ]
                col [ HtmlProperties.Span 1 ; HtmlProperties.Style [ CSSProperties.Width "80%" ] ]
            ]

            tr [ Class "table-heading" ] [
                th [] [ string "Name" ]
                th [ Scope "col" ] [ string "Aliases" ]
                th [ Scope "col" ] [ string "Admin Only?" ]
                th [ Scope "col" ] [ string "Cooldown (seconds)" ]
                th [ Scope "col" ] [ string "Description" ]
            ]
            for command in Commands.commandsMap |> List.sortBy (fun c -> c.Name) do
                tr [ Class "table-row" ] [
                    td [] [ string command.Name ]
                    td [] [ string (command.Aliases |> String.concat ",") ]
                    td [ HtmlProperties.Style [ CSSProperties.TextAlign "center" ] ] [ string (if command.AdminOnly then "✓" else "✘") ]
                    td [ HtmlProperties.Style [ CSSProperties.TextAlign "center" ] ] [ string $"{command.Cooldown / 1000}" ]
                    td [] [ pre [] [ string (System.Web.HttpUtility.HtmlEncode(command.Description)) ] ]
                ]
        ]

    let index =
        let title = h1 [ Class "article-title" ] [ string "Commands" ]

        article [ Class "articles" ] [ div [ Class "article-body" ] [ title ; commands ] ]


    Layout.layout ctx index |> Layout.render ctx


let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
