[<AutoOpen>]
module Types

type Page = string
type ProjectRoot = string

type PageConfig = {
    Page: string
    Html: string
    OutputFile: string
}

type PageGenerators = {
    Generators: PageConfig list
}
