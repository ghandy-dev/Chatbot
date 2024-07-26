module Loaders.CommandLoader

let private loadCommands =
    Chatbot.Commands.Commands.commandsList
    |> Seq.map (fun (command) ->
        {
            Title = command.Details.Name
            Description = command.Details.Description
            Command = command.Name
            Aliases = command.Aliases
            Cooldown = command.Cooldown / 1000
            AdminOnly = command.AdminOnly
            ExampleUsage = command.Details.ExampleUsage
        }
    )


let loader (projectRoot: string) (siteContent: SiteContents) =
    let commands = loadCommands
    siteContent.Add(commands)

    siteContent
