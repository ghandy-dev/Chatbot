module Build

open Config

open System
open System.IO

let private excludePaths (file: string) =

    let fileShouldBeExcluded =
        file.Contains "bin"
        || file.Contains "obj"
        || file.Contains "public"
        || file.Contains "bin"
        || file.Contains "lib"
        || file.Contains "data"
        || file.Contains "settings"
        || file.Contains "wwwroot"

    fileShouldBeExcluded |> not

let private getRelativePath (baseFolder: string) (targetFile: string) : string =
    let baseUri =
        Uri(Path.GetFullPath(baseFolder) + Path.DirectorySeparatorChar.ToString())

    let targetUri = Uri(Path.GetFullPath(targetFile))
    let relativeUri = baseUri.MakeRelativeUri(targetUri)

    let relativePath =
        Uri
            .UnescapeDataString(relativeUri.ToString())
            .Replace('/', Path.DirectorySeparatorChar)

    relativePath


let private createOutDir () =
    let outputDir = new DirectoryInfo(OutputDir)
    outputDir.Create()

let private generatePages () =
    async {
        let siteContents = Loaders.GlobalLoader.loader ProjectRoot (new SiteContents())

        let configs = Config.config

        for config in configs.Generators do
            printfn """Generating page "%s" """ config.Page
            printfn """Writing page "%s" to "%s" """ config.Page config.OutputFile
            do! File.WriteAllTextAsync($"{OutputDir}/{config.OutputFile}", config.Html) |> Async.AwaitTask
    }

let private copyCssFiles () =
    let rootDir = new DirectoryInfo(ProjectRoot)

    let cssFiles =
        rootDir.GetFiles("*.css", new EnumerationOptions(RecurseSubdirectories = true)) |> Array.filter (fun f -> excludePaths f.FullName)

    cssFiles
    |> Seq.iter (fun f ->
        let relativeFilePath = getRelativePath ProjectRoot f.FullName
        let outFilePath = new FileInfo($"{OutputFolderName}/{relativeFilePath}")

        if File.Exists(outFilePath.FullName) then
            File.Delete(outFilePath.FullName)

        if not (Directory.Exists(outFilePath.DirectoryName)) then
            Directory.CreateDirectory(outFilePath.DirectoryName) |> ignore

        printfn "Copying %s to %s" f.FullName outFilePath.FullName
        f.CopyTo(outFilePath.FullName) |> ignore
    )

let build () =
    async {
        printfn "-- Creating output dir --"
        createOutDir ()
        printfn "-- Generating pages --"
        do! generatePages ()
        printfn "-- Copying css files --"
        copyCssFiles ()
    }
