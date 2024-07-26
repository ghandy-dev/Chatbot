module Layout

open Html

open Loaders.GlobalLoader

let private layout (ctx: SiteContents) childContent =
    let title', desc =
        match ctx.TryGetValue<SiteInfo>() with
        | Some siteInfo -> siteInfo.Title, siteInfo.Description
        | None -> "", ""

    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            ``base`` [ Href "/" ]
            meta [ Name "viewport" ; Content "width=device-width, initial-scale=1" ]
            title [] [ !! $"{title'}" ]
            link [ Rel "stylesheet" ; Href "https://fonts.googleapis.com/css?family=Open+Sans" ]
            link [ Rel "stylesheet" ; Href "https://cdn.jsdelivr.net/npm/tiny.css@0.12/dist/dark.css" ]
            link [ Rel "stylesheet" ; Type "text/css" ; Href "style/style.css" ]

        ]
        body [] [
            childContent
        ]
    ]

let render (ctx: SiteContents) content = layout ctx content |> HtmlElement.ToString
