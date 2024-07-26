module Serve

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Rewrite

let serve () =
    let builder =
        WebApplication.CreateBuilder(new WebApplicationOptions(WebRootPath = Config.OutputFolderName))

    let app = builder.Build()

    let rewriteOptions = new RewriteOptions()
    rewriteOptions.AddRewrite(@"/", replacement = "index.html", skipRemainingRules = true) |> ignore
    rewriteOptions.AddRewrite(@"^(.*[^/])$", replacement = "$1.html", skipRemainingRules = true) |> ignore

    app.UseDefaultFiles() |> ignore
    app.UseRewriter(rewriteOptions) |> ignore
    app.UseStaticFiles(new StaticFileOptions()) |> ignore
    app.UseHttpsRedirection() |> ignore

    app.Run()
