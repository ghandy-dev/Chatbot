module Loaders.GlobalLoader

type SiteInfo = {
    Title: string
    Description: string
}

let loader (projectRoot: string) (siteContent: SiteContents) =
    let siteInfo: SiteInfo = {
        Title = "Commands"
        Description = "Help"
    }

    siteContent.Add(siteInfo)

    siteContent
