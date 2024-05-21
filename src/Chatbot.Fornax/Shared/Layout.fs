module Layout

open Html

open Loaders.GlobalLoader

let layout (ctx: SiteContents) childContent =
    let title', desc =
        match ctx.TryGetValue<SiteInfo>() with
        | Some siteInfo -> siteInfo.Title, siteInfo.Description
        | None -> "", ""

    html [] [
        head [] [
            meta [ CharSet "utf-8" ]
            ``base`` [ Href "/" ]
            meta [ Name "viewport" ; Content "width=device-width, initial-scale=1" ]
            title [] [ !! $"{title'} - {desc}" ]
            // link [ Rel "icon" ; Type "image/png" ; Sizes "32x32" ; Href "/images/favicon.png" ]
            link [ Rel "stylesheet" ; Href "https://fonts.googleapis.com/css?family=Open+Sans" ]
            link [ Rel "stylesheet" ; Type "text/css" ; Href "/style/style.css" ]

        ]
        body [] [ childContent ]
    ]

let render (ctx: SiteContents) content = content |> HtmlElement.ToString
