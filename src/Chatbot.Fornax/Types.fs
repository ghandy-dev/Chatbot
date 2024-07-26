[<AutoOpen>]
module Types

type Page = string
type ProjectRoot = string

type PageConfig = {
    Page: string
    GenerateOutput: string
    Output: Config.GeneratorOutput
}

type PageGenerators = {
    Generators: PageConfig list
}

type CommandPage = {
    Title: string
    Description: string
    Command: string
    Aliases: string list
    Cooldown: int
    AdminOnly: bool
    ExampleUsage: string
}