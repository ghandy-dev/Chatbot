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
            for name, command in Commands.commands |> Map.toList |> List.distinctBy (fun (name, command) -> command.Name) do
                tr [ Class "table-row" ] [
                    td [] [ string name ]
                    td [] [ string (if command.AdminOnly then "✓" else "✘") ]
                    td [] [ string (command.Aliases |> String.concat ",") ]
                    td [] [ string $"{command.Cooldown / 1000}" ]
                    td [] [ string command.Description ]
                ]
        ]

    let index =
        let title = h1 [ Class "article-title" ] [ string "Commands" ]

        article [ Class "articles" ] [ div [ Class "article-body" ] [ title ; commands ] ]


    Layout.layout ctx index |> Layout.render ctx


let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
