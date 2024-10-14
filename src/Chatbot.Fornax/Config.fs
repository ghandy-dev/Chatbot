module Config

let [<Literal>] ProjectRoot = __SOURCE_DIRECTORY__
let [<Literal>] OutputFolderName = "wwwroot"
let [<Literal>] OutputDir = ProjectRoot + "/" + OutputFolderName

let siteContents =
    (new SiteContents())
    |> Loaders.GlobalLoader.loader ProjectRoot
    |> Loaders.CommandLoader.loader ProjectRoot

let config: PageGenerators = {
    Generators = [
        yield {
            Page = "index"
            GenerateOutput = Pages.Index.generate siteContents ProjectRoot "index"
            Output = Config.NewFileName "index.html"
        }
        yield!
            Commands.Commands.commandsList |> List.map (fun (command) ->
                {
                    Page = command.Name
                    GenerateOutput = Pages.Command.generate siteContents ProjectRoot command.Name
                    Output =  Config.NewFileName $"{command.Name}/index.html"
                }
            )
    ]
}
