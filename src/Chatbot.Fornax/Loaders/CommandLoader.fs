module Loaders.CommandLoader

let private loadCommands =
    Commands.commands
    |> Seq.map (fun command ->
        {
            Title = command.HelpInfo.Name
            Description = command.HelpInfo.Description
            Command = command.Name
            Aliases = command.Aliases
            Cooldown = command.Cooldown
            AdminOnly = command.AdminOnly
            CanPipe = command.CanPipe
            ExampleUsage = command.HelpInfo.ExampleUsage
        }
    )

let loader (projectRoot: string) (siteContent: SiteContents) =
    let commands = loadCommands
    siteContent.Add(commands)

    siteContent
