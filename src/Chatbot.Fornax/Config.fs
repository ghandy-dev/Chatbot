module Config

[<Literal>]
let ProjectRoot = __SOURCE_DIRECTORY__

[<Literal>]
let OutputFolderName = "wwwroot"

[<Literal>]
let OutputDir = ProjectRoot + "/" + OutputFolderName

let siteContents =
    Loaders.GlobalLoader.loader __SOURCE_DIRECTORY__ (new SiteContents())

let config: PageGenerators = {
    Generators = [
        {
            Page = "index"
            Html = Pages.Index.generate siteContents ProjectRoot "index"
            OutputFile = "index.html"
        }
    ]
}
