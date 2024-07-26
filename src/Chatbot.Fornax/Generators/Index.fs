module Pages.Index

open Chatbot.Commands
open Html

let private generate' (ctx: SiteContents) (page: string) =
    let commands =
        table [] [
            colgroup [] [
                col []
                col []
                col []
            ]

            tr [] [
                th [] [ !! "Name" ]
                th [ Scope "col" ] [ !! "Aliases" ]
                th [ Scope "col" ] [ !! "Admin Only?" ]
                th [ Scope "col" ] [ !! "Cooldown (seconds)" ]
                th [ Scope "col" ] [ !! "Description" ]
            ]

            for command in Commands.commandsList |> List.sortBy (fun c -> c.Name) do
                tr [] [
                    td [] [ a [ Href $"/{command.Name}" ] [ !! command.Name ] ]
                    td [] [ !! (command.Aliases |> String.concat ",") ]
                    td [] [ !! (if command.AdminOnly then "✓" else "✘") ]
                    td [] [ !! $"{command.Cooldown / 1000}" ]
                    td [] [ !! command.Details.Description ]
                ]
        ]

    let content =
        let title = h1 [] [ !! "Commands" ]

        article [] [ div [] [ title ; commands ] ]


    content |> Layout.render ctx


let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
