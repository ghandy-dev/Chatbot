module Pages.Index

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
                th [ Scope "col" ] [ !! "Admin only?" ]
                th [ Scope "col" ] [ !! "Pipe?" ]
                th [ Scope "col" ] [ !! "Cooldown (seconds)" ]
                th [ Scope "col" ] [ !! "Description" ]
            ]

            let commands = Commands.commands |> List.sortBy (fun c -> c.Name)

            for command in commands do
                tr [] [
                    td [] [ a [ Href $"%s{command.Name}" ] [ !! command.Name ] ]
                    td [] [ !! (command.Aliases |> String.concat ",") ]
                    td [] [ !! (if command.AdminOnly then "✓" else "✘") ]
                    td [] [ !! (if command.CanPipe then "✓" else "✘") ]
                    td [] [ !! $"%d{command.Cooldown}" ]
                    td [] [ !! command.HelpInfo.Description ]
                ]
        ]

    let content =
        let title = h1 [] [ !! "Commands" ]

        div [] [ title ; commands ]


    content |> Layout.render ctx

let generate (ctx: SiteContents) (projectRoot: string) (page: string) = generate' ctx page
