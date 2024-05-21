module Pages.Index

open Chatbot.Commands
open Html

let private generate' (ctx: SiteContents) (page: string) =
    let commands =
        table [] [
            tr [ Class "table-heading" ] [
                th [] [ string "Name" ]
                th [] [ string "Admin Only?" ]
                th [] [ string "Aliases" ]
                th [] [ string "Cooldown (seconds)" ]
                th [] [ string "Description" ]
            ]
            for command in Commands.commands do
                tr [ Class "table-row" ] [
                    td [] [ string command.Value.Name ]
                    td [] [ string (if command.Value.AdminOnly then "✓" else "✘") ]
                    td [] [ string (command.Value.Aliases |> String.concat ",") ]
                    td [] [ string $"{command.Value.Cooldown / 1000}" ]
                    td [] [ string command.Value.Description ]
                ]
        ]

    let index =
        let title = h1 [ Class "article-title" ] [ string "Commands" ]

        article [ Class "articles" ] [ div [ Class "article-body" ] [ title ; commands ] ]


    Layout.layout ctx index |> Layout.render ctx


let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
